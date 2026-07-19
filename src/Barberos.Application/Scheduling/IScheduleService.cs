namespace Barberos.Application.Scheduling;

/// <summary>
/// Управление рабочим расписанием и периодами недоступности мастеров.
/// Изменение — сам мастер (своё) или admin (любое). Реализация — в Infrastructure.
/// </summary>
public interface IScheduleService
{
    /// <summary>Недельное расписание мастера. Бросает NotFoundException, если мастер не найден.</summary>
    Task<IReadOnlyList<ScheduleEntryDto>> GetScheduleAsync(Guid masterId, CancellationToken ct = default);

    /// <summary>Заменяет всё недельное расписание мастера набором интервалов.</summary>
    Task SetScheduleAsync(Guid masterId, SetScheduleRequest request, CancellationToken ct = default);

    /// <summary>Активные и будущие периоды недоступности мастера (EndAt в будущем).</summary>
    Task<IReadOnlyList<TimeOffDto>> ListTimeOffAsync(Guid masterId, CancellationToken ct = default);

    /// <summary>Добавляет период недоступности. Бросает NotFoundException, если мастер не найден.</summary>
    Task<TimeOffDto> AddTimeOffAsync(Guid masterId, CreateTimeOffRequest request, CancellationToken ct = default);

    /// <summary>Удаляет период недоступности. Бросает NotFoundException, если не найден.</summary>
    Task RemoveTimeOffAsync(Guid masterId, Guid timeOffId, CancellationToken ct = default);
}
