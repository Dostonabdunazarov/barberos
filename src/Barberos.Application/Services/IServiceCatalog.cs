namespace Barberos.Application.Services;

/// <summary>
/// Управление каталогом услуг. Чтение — публично, изменение — только admin.
/// Реализация — в Infrastructure.
/// </summary>
public interface IServiceCatalog
{
    /// <summary>Список услуг. При <paramref name="onlyActive"/> — только активные (для публичного каталога).</summary>
    Task<IReadOnlyList<ServiceDto>> ListAsync(bool onlyActive, CancellationToken ct = default);

    /// <summary>Услуга по id. Бросает NotFoundException, если не найдена.</summary>
    Task<ServiceDto> GetAsync(Guid id, CancellationToken ct = default);

    Task<ServiceDto> CreateAsync(CreateServiceRequest request, CancellationToken ct = default);

    /// <summary>Обновление услуги. Бросает NotFoundException, если не найдена.</summary>
    Task<ServiceDto> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken ct = default);
}
