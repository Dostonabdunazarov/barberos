using Barberos.Application.Abstractions;
using Barberos.Application.Availability;
using Barberos.Application.Common;
using Barberos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Barberos.Infrastructure.Scheduling;

/// <summary>
/// Расчёт свободных слотов для (мастер, услуга, дата). Алгоритм — PLAN.md §6:
/// рабочие часы дня → минус time-off → минус занятые брони → нарезка по сетке →
/// фильтр прошедшего времени и lead time.
/// </summary>
public sealed class AvailabilityService(
    IAppDbContext db,
    IOptions<BarbershopOptions> options) : IAvailabilityService
{
    private readonly BarbershopOptions _opt = options.Value;

    // Статусы, занимающие слот (cancelled/no_show освобождают).
    private static readonly BookingStatus[] BusyStatuses = [BookingStatus.Confirmed, BookingStatus.Completed];

    public async Task<AvailabilityDto> GetAvailabilityAsync(
        Guid masterId, Guid serviceId, DateOnly date, CancellationToken ct = default)
    {
        var master = await db.Masters.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == masterId && m.IsActive, ct)
            ?? throw new NotFoundException("Мастер не найден.");

        var service = await db.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive, ct)
            ?? throw new NotFoundException("Услуга не найдена.");

        var offersService = await db.MasterServices.AsNoTracking()
            .AnyAsync(ms => ms.MasterId == masterId && ms.ServiceId == serviceId, ct);
        if (!offersService)
            throw new NotFoundException("Мастер не оказывает выбранную услугу.");

        var tz = ResolveTimeZone();
        var now = DateTime.UtcNow;
        var earliestStart = now.AddMinutes(_opt.LeadTimeMinutes);

        // Длительность занятости мастера = услуга + буфер.
        var slotDuration = TimeSpan.FromMinutes(service.DurationMinutes + service.BufferMinutes);
        var step = TimeSpan.FromMinutes(_opt.SlotStepMinutes);

        // 1. Рабочие интервалы на день недели, конвертированные в UTC-инстанты этой даты.
        var scheduleEntries = await db.Schedules.AsNoTracking()
            .Where(s => s.MasterId == masterId && s.DayOfWeek == date.DayOfWeek)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

        if (scheduleEntries.Count == 0)
            return new AvailabilityDto(date, []);

        var workIntervals = scheduleEntries
            .Select(e => (
                Start: ToUtc(date, e.StartTime, tz),
                End: ToUtc(date, e.EndTime, tz)))
            .ToList();

        var dayStartUtc = workIntervals.Min(w => w.Start);
        var dayEndUtc = workIntervals.Max(w => w.End);

        // 2 + 3. Занятые интервалы: time-off и активные брони, пересекающие рабочий день.
        var timeOffs = await db.TimeOffs.AsNoTracking()
            .Where(t => t.MasterId == masterId && t.StartAt < dayEndUtc && t.EndAt > dayStartUtc)
            .Select(t => new { t.StartAt, t.EndAt })
            .ToListAsync(ct);

        var bookings = await db.Bookings.AsNoTracking()
            .Where(bk => bk.MasterId == masterId
                && BusyStatuses.Contains(bk.Status)
                && bk.StartAt < dayEndUtc && bk.EndAt > dayStartUtc)
            .Select(bk => new { bk.StartAt, bk.EndAt })
            .ToListAsync(ct);

        var busy = timeOffs.Select(t => (Start: t.StartAt, End: t.EndAt))
            .Concat(bookings.Select(b => (Start: b.StartAt, End: b.EndAt)))
            .ToList();

        // 4 + 5. Нарезка каждого рабочего интервала по сетке от его начала.
        var slots = new List<SlotDto>();
        foreach (var (workStart, workEnd) in workIntervals.OrderBy(w => w.Start))
        {
            for (var slotStart = workStart; slotStart + slotDuration <= workEnd; slotStart += step)
            {
                var slotEnd = slotStart + slotDuration;

                // Прошедшее время + lead time.
                if (slotStart < earliestStart)
                    continue;

                // Пересечение с занятыми интервалами.
                if (busy.Any(b => Overlaps(slotStart, slotEnd, b.Start, b.End)))
                    continue;

                slots.Add(new SlotDto(slotStart, slotEnd));
            }
        }

        return new AvailabilityDto(date, slots);
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(_opt.TimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // Целевая зона (Asia/Tashkent, UTC+5) без перехода часов — безопасный фолбэк.
            return TimeZoneInfo.CreateCustomTimeZone("Barbershop", TimeSpan.FromHours(5), "Barbershop", "Barbershop");
        }
    }

    /// <summary>Локальные дата+время (зона барбершопа) → UTC-инстант.</summary>
    private static DateTime ToUtc(DateOnly date, TimeOnly time, TimeZoneInfo tz)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }

    private static bool Overlaps(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        => aStart < bEnd && bStart < aEnd;
}
