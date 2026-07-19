namespace Barberos.Application.Analytics;

/// <summary>
/// Базовая админ-аналитика (только admin, проверяется policy на уровне API).
/// Реализация — в Infrastructure.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Сводка за период: общее число броней, разбивка по статусам,
    /// загрузка мастеров и популярные услуги.
    /// </summary>
    Task<AnalyticsOverviewDto> GetOverviewAsync(AnalyticsQuery query, CancellationToken ct = default);
}
