using Barberos.Domain.Enums;

namespace Barberos.Application.Analytics;

/// <summary>
/// Период выборки аналитики (по StartAt брони, UTC). Обе границы опциональны:
/// From — включительно, To — исключительно. null — без ограничения.
/// </summary>
public record AnalyticsQuery(DateTime? From, DateTime? To);

/// <summary>Количество броней в разрезе одного статуса.</summary>
public record StatusCountDto(BookingStatus Status, int Count);

/// <summary>Загрузка одного мастера за период: число броней и суммарные занятые минуты.</summary>
public record MasterLoadDto(Guid MasterId, string MasterName, int Bookings, int BusyMinutes);

/// <summary>Популярность услуги за период: число броней.</summary>
public record ServicePopularityDto(Guid ServiceId, string ServiceName, int Bookings);

/// <summary>
/// Сводка базовой админ-аналитики за период:
/// брони по статусам, загрузка мастеров, популярные услуги.
/// </summary>
public record AnalyticsOverviewDto(
    DateTime? From,
    DateTime? To,
    int TotalBookings,
    IReadOnlyList<StatusCountDto> ByStatus,
    IReadOnlyList<MasterLoadDto> MasterLoad,
    IReadOnlyList<ServicePopularityDto> PopularServices);
