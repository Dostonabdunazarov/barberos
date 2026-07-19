namespace Barberos.Application.Masters;

/// <summary>
/// Управление мастерами и их услугами. Чтение — публично, изменение — только admin.
/// Реализация — в Infrastructure.
/// </summary>
public interface IMasterCatalog
{
    /// <summary>Список мастеров. При <paramref name="onlyActive"/> — только активные.</summary>
    Task<IReadOnlyList<MasterDto>> ListAsync(bool onlyActive, CancellationToken ct = default);

    /// <summary>Мастер по id. Бросает NotFoundException, если не найден.</summary>
    Task<MasterDto> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Создаёт мастера и (опционально) его учётную запись.
    /// Бросает ConflictException, если email уже занят; NotFoundException, если услуга не существует.
    /// </summary>
    Task<MasterDto> CreateAsync(CreateMasterRequest request, CancellationToken ct = default);

    /// <summary>Обновление профиля и набора услуг. Бросает NotFoundException, если не найден.</summary>
    Task<MasterDto> UpdateAsync(Guid id, UpdateMasterRequest request, CancellationToken ct = default);
}
