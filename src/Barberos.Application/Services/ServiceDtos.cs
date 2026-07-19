namespace Barberos.Application.Services;

/// <summary>Публичное представление услуги.</summary>
public record ServiceDto(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    int BufferMinutes,
    decimal Price,
    bool IsActive);

/// <summary>Данные для создания услуги (admin).</summary>
public record CreateServiceRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    int BufferMinutes,
    decimal Price);

/// <summary>Данные для обновления услуги (admin). IsActive управляет доступностью.</summary>
public record UpdateServiceRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    int BufferMinutes,
    decimal Price,
    bool IsActive);
