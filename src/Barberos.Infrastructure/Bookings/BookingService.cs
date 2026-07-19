using Barberos.Application.Bookings;
using Barberos.Application.Common;
using Barberos.Domain.Entities;
using Barberos.Domain.Enums;
using Barberos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Barberos.Infrastructure.Bookings;

/// <summary>
/// Операции над бронями поверх EF Core. Создание и перенос — в транзакции с защитой
/// от двойного бронирования: EXCLUDE-constraint на уровне БД (см. миграцию) плюс
/// прикладная проверка пересечений (страховка и корректная работа на in-memory в тестах).
/// </summary>
public sealed class BookingService(
    AppDbContext db,
    IOptions<BarbershopOptions> options) : IBookingService
{
    private readonly BarbershopOptions _opt = options.Value;

    /// <summary>SQLSTATE нарушения EXCLUDE-constraint (exclusion_violation).</summary>
    private const string ExclusionViolation = "23P01";

    // Статусы, занимающие слот (совпадают с фильтром EXCLUDE-constraint и расчётом слотов).
    private static readonly BookingStatus[] BusyStatuses = [BookingStatus.Confirmed, BookingStatus.Completed];

    // ── Создание (публично) ────────────────────────────────────────────────────

    public async Task<CreateBookingResult> CreateAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        var master = await db.Masters.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MasterId && m.IsActive, ct)
            ?? throw new NotFoundException("Мастер не найден.");

        var service = await db.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, ct)
            ?? throw new NotFoundException("Услуга не найдена.");

        var offersService = await db.MasterServices.AsNoTracking()
            .AnyAsync(ms => ms.MasterId == master.Id && ms.ServiceId == service.Id, ct);
        if (!offersService)
            throw new NotFoundException("Мастер не оказывает выбранную услугу.");

        var startAt = DateTime.SpecifyKind(request.StartAt, DateTimeKind.Utc);
        var endAt = startAt.AddMinutes(service.DurationMinutes + service.BufferMinutes);

        await ValidateSlotAsync(master.Id, service, startAt, endAt, ct);

        var booking = new Booking
        {
            GuestName = request.GuestName.Trim(),
            GuestPhone = request.GuestPhone.Trim(),
            MasterId = master.Id,
            ServiceId = service.Id,
            StartAt = startAt,
            EndAt = endAt,
            Status = BookingStatus.Confirmed,
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await EnsureNoOverlapAsync(master.Id, startAt, endAt, excludeBookingId: null, ct);
            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsExclusionViolation(ex))
        {
            await tx.RollbackAsync(ct);
            throw new ConflictException("Выбранное время только что заняли. Выберите другой слот.");
        }

        return new CreateBookingResult(booking.Id, booking.ManageToken);
    }

    // ── Просмотр клиентом по токену (публично, только чтение) ────────────────────

    public async Task<BookingManageDto> GetByManageTokenAsync(Guid manageToken, CancellationToken ct = default)
    {
        var dto = await db.Bookings.AsNoTracking()
            .Where(b => b.ManageToken == manageToken)
            .Select(b => new BookingManageDto(
                b.Id,
                b.GuestName,
                b.Master.Name,
                b.Service.Name,
                b.StartAt,
                b.EndAt,
                b.Service.Price,
                b.Status))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Бронь не найдена.");

        return dto;
    }

    // ── Список для персонала ─────────────────────────────────────────────────────

    public async Task<BookingPageDto> ListAsync(BookingQuery query, StaffContext caller, CancellationToken ct = default)
    {
        var q = db.Bookings.AsNoTracking().AsQueryable();

        // Мастер видит только свои брони; админ — все (или отфильтрованные по MasterId).
        if (!caller.IsAdmin)
        {
            var myMasterId = await ResolveCallerMasterIdAsync(caller, ct);
            q = q.Where(b => b.MasterId == myMasterId);
        }
        else if (query.MasterId is { } masterId)
        {
            q = q.Where(b => b.MasterId == masterId);
        }

        if (query.From is { } from)
            q = q.Where(b => b.StartAt >= DateTime.SpecifyKind(from, DateTimeKind.Utc));
        if (query.To is { } to)
            q = q.Where(b => b.StartAt < DateTime.SpecifyKind(to, DateTimeKind.Utc));
        if (query.Status is { } status)
            q = q.Where(b => b.Status == status);

        var total = await q.CountAsync(ct);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        // Проекция инлайн (а не через ToDto): EF подтягивает навигации Master/Service.
        var items = await q
            .OrderBy(b => b.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookingDto(
                b.Id,
                b.GuestName,
                b.GuestPhone,
                b.MasterId,
                b.Master.Name,
                b.ServiceId,
                b.Service.Name,
                b.StartAt,
                b.EndAt,
                b.Status,
                b.CreatedAt))
            .ToListAsync(ct);

        return new BookingPageDto(items, page, pageSize, total);
    }

    public async Task<BookingDto> GetAsync(Guid id, StaffContext caller, CancellationToken ct = default)
    {
        var booking = await db.Bookings.AsNoTracking()
            .Include(b => b.Master)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException("Бронь не найдена.");

        await EnsureCanAccessAsync(booking, caller, ct);
        return ToDto(booking);
    }

    // ── Смена статуса ────────────────────────────────────────────────────────────

    public async Task<BookingDto> UpdateStatusAsync(
        Guid id, UpdateBookingStatusRequest request, StaffContext caller, CancellationToken ct = default)
    {
        var booking = await db.Bookings
            .Include(b => b.Master)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException("Бронь не найдена.");

        await EnsureCanAccessAsync(booking, caller, ct);

        if (!IsAllowedTransition(booking.Status, request.Status))
            throw new ConflictException(
                $"Недопустимый переход статуса: {booking.Status} → {request.Status}.");

        booking.Status = request.Status;
        await db.SaveChangesAsync(ct);

        return ToDto(booking);
    }

    // ── Перенос ──────────────────────────────────────────────────────────────────

    public async Task<BookingDto> RescheduleAsync(
        Guid id, RescheduleBookingRequest request, StaffContext caller, CancellationToken ct = default)
    {
        var booking = await db.Bookings
            .Include(b => b.Master)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException("Бронь не найдена.");

        await EnsureCanAccessAsync(booking, caller, ct);

        if (booking.Status != BookingStatus.Confirmed)
            throw new ConflictException("Переносить можно только подтверждённую бронь.");

        var masterId = request.NewMasterId ?? booking.MasterId;

        var service = request.NewServiceId is { } newServiceId
            ? await db.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == newServiceId && s.IsActive, ct)
                ?? throw new NotFoundException("Услуга не найдена.")
            : booking.Service;

        if (masterId != booking.MasterId)
        {
            var master = await db.Masters.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == masterId && m.IsActive, ct)
                ?? throw new NotFoundException("Мастер не найден.");
        }

        // Мастер должен оказывать (возможно новую) услугу.
        var offersService = await db.MasterServices.AsNoTracking()
            .AnyAsync(ms => ms.MasterId == masterId && ms.ServiceId == service.Id, ct);
        if (!offersService)
            throw new NotFoundException("Мастер не оказывает выбранную услугу.");

        var startAt = DateTime.SpecifyKind(request.NewStartAt, DateTimeKind.Utc);
        var endAt = startAt.AddMinutes(service.DurationMinutes + service.BufferMinutes);

        await ValidateSlotAsync(masterId, service, startAt, endAt, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await EnsureNoOverlapAsync(masterId, startAt, endAt, excludeBookingId: booking.Id, ct);

            booking.MasterId = masterId;
            booking.ServiceId = service.Id;
            booking.StartAt = startAt;
            booking.EndAt = endAt;

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsExclusionViolation(ex))
        {
            await tx.RollbackAsync(ct);
            throw new ConflictException("Выбранное время занято. Выберите другой слот.");
        }

        // Перечитываем связанные сущности для корректного DTO (мастер/услуга могли смениться).
        await db.Entry(booking).Reference(b => b.Master).LoadAsync(ct);
        await db.Entry(booking).Reference(b => b.Service).LoadAsync(ct);
        return ToDto(booking);
    }

    // ── Общая проверка слота (расписание + time-off + lead time) ────────────────

    /// <summary>
    /// Проверяет, что интервал [startAt, endAt) попадает в рабочие часы мастера в этот день,
    /// не пересекается с time-off и удовлетворяет lead time. Пересечение с чужими бронями
    /// проверяется отдельно под транзакцией (EnsureNoOverlapAsync + EXCLUDE-constraint).
    /// </summary>
    private async Task ValidateSlotAsync(
        Guid masterId, Service service, DateTime startAt, DateTime endAt, CancellationToken ct)
    {
        var earliest = DateTime.UtcNow.AddMinutes(_opt.LeadTimeMinutes);
        if (startAt < earliest)
            throw new ConflictException("Выбранное время уже недоступно (слишком поздно для записи).");

        var tz = ResolveTimeZone();
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(startAt, tz);
        var date = DateOnly.FromDateTime(localStart);

        // Интервал должен целиком укладываться в один рабочий интервал этого дня недели.
        var scheduleEntries = await db.Schedules.AsNoTracking()
            .Where(s => s.MasterId == masterId && s.DayOfWeek == date.DayOfWeek)
            .Select(s => new { s.StartTime, s.EndTime })
            .ToListAsync(ct);

        var fitsSchedule = scheduleEntries.Any(e =>
        {
            var workStart = ToUtc(date, e.StartTime, tz);
            var workEnd = ToUtc(date, e.EndTime, tz);
            return startAt >= workStart && endAt <= workEnd;
        });
        if (!fitsSchedule)
            throw new ConflictException("Выбранное время вне рабочих часов мастера.");

        var hitsTimeOff = await db.TimeOffs.AsNoTracking()
            .AnyAsync(t => t.MasterId == masterId && t.StartAt < endAt && t.EndAt > startAt, ct);
        if (hitsTimeOff)
            throw new ConflictException("Выбранное время недоступно (перерыв/отпуск мастера).");
    }

    /// <summary>Прикладная проверка пересечения с занятыми бронями мастера (без учёта excludeBookingId).</summary>
    private async Task EnsureNoOverlapAsync(
        Guid masterId, DateTime startAt, DateTime endAt, Guid? excludeBookingId, CancellationToken ct)
    {
        var overlaps = await db.Bookings.AsNoTracking()
            .AnyAsync(b => b.MasterId == masterId
                && (excludeBookingId == null || b.Id != excludeBookingId)
                && BusyStatuses.Contains(b.Status)
                && b.StartAt < endAt && b.EndAt > startAt, ct);

        if (overlaps)
            throw new ConflictException("Выбранное время только что заняли. Выберите другой слот.");
    }

    // ── Авторизация персонала ────────────────────────────────────────────────────

    private async Task EnsureCanAccessAsync(Booking booking, StaffContext caller, CancellationToken ct)
    {
        if (caller.IsAdmin)
            return;

        var myMasterId = await ResolveCallerMasterIdAsync(caller, ct);
        if (booking.MasterId != myMasterId)
            throw new ForbiddenException("Нет доступа к брони другого мастера.");
    }

    /// <summary>Id мастера, привязанного к учётке вызывающего. Forbidden, если учётка не мастер.</summary>
    private async Task<Guid> ResolveCallerMasterIdAsync(StaffContext caller, CancellationToken ct)
    {
        var masterId = await db.Masters.AsNoTracking()
            .Where(m => m.UserId == caller.UserId)
            .Select(m => (Guid?)m.Id)
            .FirstOrDefaultAsync(ct);

        return masterId ?? throw new ForbiddenException("Учётная запись не привязана к мастеру.");
    }

    // ── Правила переходов статуса ────────────────────────────────────────────────

    private static bool IsAllowedTransition(BookingStatus from, BookingStatus to) => from switch
    {
        // Из подтверждённой можно завершить, отметить неявку или отменить.
        BookingStatus.Confirmed => to is BookingStatus.Completed
            or BookingStatus.NoShow
            or BookingStatus.Cancelled,
        // Финальные статусы не меняются.
        _ => false,
    };

    // ── Утилиты времени/зоны (согласованы с AvailabilityService) ─────────────────

    private TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(_opt.TimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("Barbershop", TimeSpan.FromHours(5), "Barbershop", "Barbershop");
        }
    }

    private static DateTime ToUtc(DateOnly date, TimeOnly time, TimeZoneInfo tz)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }

    private static bool IsExclusionViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == ExclusionViolation;

    private static BookingDto ToDto(Booking b) => new(
        b.Id,
        b.GuestName,
        b.GuestPhone,
        b.MasterId,
        b.Master.Name,
        b.ServiceId,
        b.Service.Name,
        b.StartAt,
        b.EndAt,
        b.Status,
        b.CreatedAt);
}
