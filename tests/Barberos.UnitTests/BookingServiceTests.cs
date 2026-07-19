using Barberos.Application.Bookings;
using Barberos.Application.Common;
using Barberos.Domain.Entities;
using Barberos.Domain.Enums;
using Barberos.Infrastructure.Bookings;
using Barberos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace Barberos.UnitTests;

/// <summary>
/// Тесты бронирования (PLAN.md §Этап 3). Зона барбершопа — Asia/Tashkent (UTC+5).
/// InMemory-провайдер не применяет EXCLUDE-constraint, поэтому защита от пересечений
/// проверяется прикладной проверкой (EnsureNoOverlapAsync); токен-конфликт БД — интеграционно.
/// </summary>
public class BookingServiceTests
{
    private const string Tz = "Asia/Tashkent";
    private static readonly Guid MasterId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherMasterId = Guid.Parse("1a1a1a1a-1111-1111-1111-111111111111");
    private static readonly Guid ServiceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid MasterUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid OtherUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    // 2030-01-07 — понедельник, далеко в будущем (lead time не мешает).
    private static readonly DateOnly FutureMonday = new(2030, 1, 7);

    private static readonly StaffContext AdminCtx = new(Guid.NewGuid(), IsAdmin: true);
    private static readonly StaffContext MasterCtx = new(MasterUserId, IsAdmin: false);

    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // InMemory не поддерживает транзакции — сервис бронирования их использует;
            // игнорируем предупреждение (в проде провайдер Npgsql, транзакции реальны).
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static BookingService NewService(AppDbContext db, int leadTimeMinutes = 120)
    {
        var opts = Options.Create(new BarbershopOptions
        {
            TimeZone = Tz,
            SlotStepMinutes = 15,
            LeadTimeMinutes = leadTimeMinutes,
        });
        return new BookingService(db, opts);
    }

    /// <summary>Мастер (с учёткой), услуга (30 мин + buffer), связь и смена 09:00–18:00 в понедельник.</summary>
    private static void Seed(AppDbContext db, int durationMin = 30, int bufferMin = 0)
    {
        db.Masters.Add(new Master { Id = MasterId, Name = "Тест", UserId = MasterUserId, IsActive = true });
        db.Services.Add(new Service
        {
            Id = ServiceId,
            Name = "Стрижка",
            DurationMinutes = durationMin,
            BufferMinutes = bufferMin,
            Price = 100_000m,
            IsActive = true,
        });
        db.MasterServices.Add(new MasterService { MasterId = MasterId, ServiceId = ServiceId });
        db.Schedules.Add(new Schedule
        {
            MasterId = MasterId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(18, 0),
        });
    }

    private static DateTime LocalToUtc(DateOnly date, int hour, int minute)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Tz);
        var local = DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(hour, minute)), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }

    private static CreateBookingRequest CreateReq(DateTime startUtc) =>
        new("Иван", "+998901234567", MasterId, ServiceId, startUtc);

    // ── Создание ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidSlot_PersistsConfirmedBookingWithToken()
    {
        using var db = NewDb();
        Seed(db, durationMin: 30, bufferMin: 15);
        await db.SaveChangesAsync();

        var start = LocalToUtc(FutureMonday, 10, 0);
        var result = await NewService(db).CreateAsync(CreateReq(start));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotEqual(Guid.Empty, result.ManageToken);

        var saved = await db.Bookings.SingleAsync();
        Assert.Equal(BookingStatus.Confirmed, saved.Status);
        Assert.Equal(start, saved.StartAt);
        // EndAt = start + duration + buffer = +45 мин.
        Assert.Equal(start.AddMinutes(45), saved.EndAt);
        Assert.Equal("Иван", saved.GuestName);
    }

    [Fact]
    public async Task Create_MasterDoesNotOfferService_ThrowsNotFound()
    {
        using var db = NewDb();
        db.Masters.Add(new Master { Id = MasterId, Name = "Тест", IsActive = true });
        db.Services.Add(new Service { Id = ServiceId, Name = "Стрижка", DurationMinutes = 30, IsActive = true });
        // Нет связи MasterService.
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            NewService(db).CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 0))));
    }

    [Fact]
    public async Task Create_OutsideWorkingHours_ThrowsConflict()
    {
        using var db = NewDb();
        Seed(db);
        await db.SaveChangesAsync();

        // 08:00 локально — до начала смены (09:00).
        await Assert.ThrowsAsync<ConflictException>(() =>
            NewService(db).CreateAsync(CreateReq(LocalToUtc(FutureMonday, 8, 0))));
    }

    [Fact]
    public async Task Create_WithinLeadTime_ThrowsConflict()
    {
        using var db = NewDb();
        Seed(db);
        // Смена сегодня, весь день.
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Tz);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
        db.Schedules.Add(new Schedule
        {
            MasterId = MasterId,
            DayOfWeek = today.DayOfWeek,
            StartTime = new TimeOnly(0, 0),
            EndTime = new TimeOnly(23, 30),
        });
        await db.SaveChangesAsync();

        // Через 10 минут — заведомо меньше lead time 120.
        var soon = DateTime.UtcNow.AddMinutes(10);
        await Assert.ThrowsAsync<ConflictException>(() => NewService(db).CreateAsync(CreateReq(soon)));
    }

    [Fact]
    public async Task Create_OverlappingConfirmedBooking_ThrowsConflict()
    {
        using var db = NewDb();
        Seed(db, durationMin: 30);
        db.Bookings.Add(new Booking
        {
            MasterId = MasterId,
            ServiceId = ServiceId,
            GuestName = "Первый",
            GuestPhone = "+998900000000",
            StartAt = LocalToUtc(FutureMonday, 10, 0),
            EndAt = LocalToUtc(FutureMonday, 10, 30),
            Status = BookingStatus.Confirmed,
        });
        await db.SaveChangesAsync();

        // 10:15 пересекает существующую 10:00–10:30.
        await Assert.ThrowsAsync<ConflictException>(() =>
            NewService(db).CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 15))));
    }

    [Fact]
    public async Task Create_OverCancelledBooking_Succeeds()
    {
        using var db = NewDb();
        Seed(db, durationMin: 30);
        db.Bookings.Add(new Booking
        {
            MasterId = MasterId,
            ServiceId = ServiceId,
            GuestName = "Отменённый",
            GuestPhone = "+998900000000",
            StartAt = LocalToUtc(FutureMonday, 10, 0),
            EndAt = LocalToUtc(FutureMonday, 10, 30),
            Status = BookingStatus.Cancelled, // освобождает слот
        });
        await db.SaveChangesAsync();

        var result = await NewService(db).CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 0)));
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Create_DuringTimeOff_ThrowsConflict()
    {
        using var db = NewDb();
        Seed(db);
        db.TimeOffs.Add(new TimeOff
        {
            MasterId = MasterId,
            StartAt = LocalToUtc(FutureMonday, 9, 0),
            EndAt = LocalToUtc(FutureMonday, 12, 0),
            Reason = "Перерыв",
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            NewService(db).CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 0))));
    }

    // ── Просмотр по токену ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByManageToken_ReturnsBooking_WithoutPhone()
    {
        using var db = NewDb();
        Seed(db);
        await db.SaveChangesAsync();
        var created = await NewService(db).CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 0)));

        var dto = await NewService(db).GetByManageTokenAsync(created.ManageToken);

        Assert.Equal(created.Id, dto.Id);
        Assert.Equal("Стрижка", dto.ServiceName);
        Assert.Equal("Тест", dto.MasterName);
        Assert.Equal(100_000m, dto.Price);
    }

    [Fact]
    public async Task GetByManageToken_Unknown_ThrowsNotFound()
    {
        using var db = NewDb();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            NewService(db).GetByManageTokenAsync(Guid.NewGuid()));
    }

    // ── Статусы ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ConfirmedToCompleted_Succeeds()
    {
        using var db = NewDb();
        Seed(db);
        await db.SaveChangesAsync();
        var created = await NewService(db).CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 0)));

        var dto = await NewService(db).UpdateStatusAsync(
            created.Id, new UpdateBookingStatusRequest(BookingStatus.Completed), AdminCtx);

        Assert.Equal(BookingStatus.Completed, dto.Status);
    }

    [Fact]
    public async Task UpdateStatus_FromFinalStatus_ThrowsConflict()
    {
        using var db = NewDb();
        Seed(db);
        await db.SaveChangesAsync();
        var svc = NewService(db);
        var created = await svc.CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 0)));
        await svc.UpdateStatusAsync(created.Id, new UpdateBookingStatusRequest(BookingStatus.Cancelled), AdminCtx);

        // Из cancelled переход запрещён.
        await Assert.ThrowsAsync<ConflictException>(() => NewService(db).UpdateStatusAsync(
            created.Id, new UpdateBookingStatusRequest(BookingStatus.Completed), AdminCtx));
    }

    [Fact]
    public async Task UpdateStatus_ByForeignMaster_ThrowsForbidden()
    {
        using var db = NewDb();
        Seed(db);
        db.Masters.Add(new Master { Id = OtherMasterId, Name = "Другой", UserId = OtherUserId, IsActive = true });
        await db.SaveChangesAsync();
        var created = await NewService(db).CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 0)));

        var foreignCtx = new StaffContext(OtherUserId, IsAdmin: false);
        await Assert.ThrowsAsync<ForbiddenException>(() => NewService(db).UpdateStatusAsync(
            created.Id, new UpdateBookingStatusRequest(BookingStatus.Completed), foreignCtx));
    }

    // ── Перенос ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reschedule_ToFreeSlot_MovesBooking()
    {
        using var db = NewDb();
        Seed(db, durationMin: 30);
        await db.SaveChangesAsync();
        var created = await NewService(db).CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 0)));

        var newStart = LocalToUtc(FutureMonday, 14, 0);
        var dto = await NewService(db).RescheduleAsync(
            created.Id, new RescheduleBookingRequest(newStart, null, null), MasterCtx);

        Assert.Equal(newStart, dto.StartAt);
        Assert.Equal(newStart.AddMinutes(30), dto.EndAt);
    }

    [Fact]
    public async Task Reschedule_OntoAnotherBooking_ThrowsConflict()
    {
        using var db = NewDb();
        Seed(db, durationMin: 30);
        await db.SaveChangesAsync();
        var svc = NewService(db);
        var a = await svc.CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 0)));
        await NewService(db).CreateAsync(CreateReq(LocalToUtc(FutureMonday, 11, 0)));

        // Переносим A на 11:00 — занято вторым.
        await Assert.ThrowsAsync<ConflictException>(() => NewService(db).RescheduleAsync(
            a.Id, new RescheduleBookingRequest(LocalToUtc(FutureMonday, 11, 0), null, null), AdminCtx));
    }

    [Fact]
    public async Task Reschedule_CancelledBooking_ThrowsConflict()
    {
        using var db = NewDb();
        Seed(db);
        await db.SaveChangesAsync();
        var svc = NewService(db);
        var created = await svc.CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 0)));
        await svc.UpdateStatusAsync(created.Id, new UpdateBookingStatusRequest(BookingStatus.Cancelled), AdminCtx);

        await Assert.ThrowsAsync<ConflictException>(() => NewService(db).RescheduleAsync(
            created.Id, new RescheduleBookingRequest(LocalToUtc(FutureMonday, 14, 0), null, null), AdminCtx));
    }

    // ── Список ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_Master_SeesOnlyOwnBookings()
    {
        using var db = NewDb();
        Seed(db, durationMin: 30);
        db.Masters.Add(new Master { Id = OtherMasterId, Name = "Другой", UserId = OtherUserId, IsActive = true });
        db.Services.Add(new Service { Id = Guid.NewGuid(), Name = "X", DurationMinutes = 30, IsActive = true });
        await db.SaveChangesAsync();

        await NewService(db).CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 0)));
        // Бронь другого мастера (напрямую, минуя валидацию).
        db.Bookings.Add(new Booking
        {
            MasterId = OtherMasterId,
            ServiceId = ServiceId,
            GuestName = "Чужой",
            GuestPhone = "+998900000001",
            StartAt = LocalToUtc(FutureMonday, 10, 0),
            EndAt = LocalToUtc(FutureMonday, 10, 30),
            Status = BookingStatus.Confirmed,
        });
        await db.SaveChangesAsync();

        var page = await NewService(db).ListAsync(new BookingQuery(null, null, null, null), MasterCtx);

        Assert.Single(page.Items);
        Assert.All(page.Items, b => Assert.Equal(MasterId, b.MasterId));
    }

    [Fact]
    public async Task List_Admin_FiltersByStatus()
    {
        using var db = NewDb();
        Seed(db, durationMin: 30);
        await db.SaveChangesAsync();
        var svc = NewService(db);
        var a = await svc.CreateAsync(CreateReq(LocalToUtc(FutureMonday, 10, 0)));
        await NewService(db).CreateAsync(CreateReq(LocalToUtc(FutureMonday, 11, 0)));
        await NewService(db).UpdateStatusAsync(a.Id, new UpdateBookingStatusRequest(BookingStatus.Cancelled), AdminCtx);

        var confirmed = await NewService(db).ListAsync(
            new BookingQuery(null, null, null, BookingStatus.Confirmed), AdminCtx);

        Assert.Single(confirmed.Items);
        Assert.Equal(BookingStatus.Confirmed, confirmed.Items[0].Status);
    }
}
