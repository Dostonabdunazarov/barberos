using Barberos.Application.Abstractions;
using Barberos.Application.Analytics;
using Barberos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Barberos.Infrastructure.Analytics;

/// <summary>
/// Базовая аналитика поверх EF Core. Все агрегаты — за период по StartAt (UTC).
/// Разбивка по статусам — по всем броням; загрузка мастеров и популярность услуг —
/// по броням, реально занимавшим слот (Confirmed/Completed).
/// </summary>
public sealed class AnalyticsService(IAppDbContext db) : IAnalyticsService
{
    // Статусы, реально занимавшие время мастера (согласовано с BookingService.BusyStatuses).
    private static readonly BookingStatus[] BusyStatuses = [BookingStatus.Confirmed, BookingStatus.Completed];

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(AnalyticsQuery query, CancellationToken ct = default)
    {
        var scope = db.Bookings.AsNoTracking();

        if (query.From is { } from)
            scope = scope.Where(b => b.StartAt >= DateTime.SpecifyKind(from, DateTimeKind.Utc));
        if (query.To is { } to)
            scope = scope.Where(b => b.StartAt < DateTime.SpecifyKind(to, DateTimeKind.Utc));

        var total = await scope.CountAsync(ct);

        // Брони по статусам (все статусы за период).
        var byStatus = await scope
            .GroupBy(b => b.Status)
            .Select(g => new StatusCountDto(g.Key, g.Count()))
            .ToListAsync(ct);
        byStatus = byStatus.OrderBy(s => s.Status).ToList();

        var busy = scope.Where(b => BusyStatuses.Contains(b.Status));

        // Загрузка мастеров: число броней + суммарные занятые минуты (EndAt-StartAt, с буфером).
        var masterLoadRaw = await busy
            .GroupBy(b => new { b.MasterId, b.Master.Name })
            .Select(g => new
            {
                g.Key.MasterId,
                g.Key.Name,
                Bookings = g.Count(),
                // EF транслирует сумму разностей интервалов; для InMemory считается в памяти.
                BusyMinutes = g.Sum(b => (int)(b.EndAt - b.StartAt).TotalMinutes),
            })
            .ToListAsync(ct);

        var masterLoad = masterLoadRaw
            .OrderByDescending(m => m.Bookings)
            .ThenBy(m => m.Name)
            .Select(m => new MasterLoadDto(m.MasterId, m.Name, m.Bookings, m.BusyMinutes))
            .ToList();

        // Популярные услуги: число броней за период.
        var popularRaw = await busy
            .GroupBy(b => new { b.ServiceId, b.Service.Name })
            .Select(g => new { g.Key.ServiceId, g.Key.Name, Bookings = g.Count() })
            .ToListAsync(ct);

        var popular = popularRaw
            .OrderByDescending(s => s.Bookings)
            .ThenBy(s => s.Name)
            .Select(s => new ServicePopularityDto(s.ServiceId, s.Name, s.Bookings))
            .ToList();

        return new AnalyticsOverviewDto(query.From, query.To, total, byStatus, masterLoad, popular);
    }
}
