namespace Barberos.Application.Availability;

/// <summary>
/// Расчёт свободных слотов для (мастер, услуга, дата). Публичный доступ.
/// Реализация — в Infrastructure. См. алгоритм в PLAN.md §6.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Доступные слоты на указанную дату (в зоне барбершопа) для мастера и услуги.
    /// Бросает NotFoundException, если мастер/услуга не найдены или мастер не оказывает услугу.
    /// </summary>
    Task<AvailabilityDto> GetAvailabilityAsync(
        Guid masterId, Guid serviceId, DateOnly date, CancellationToken ct = default);
}
