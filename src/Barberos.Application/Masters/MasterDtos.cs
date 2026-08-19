namespace Barberos.Application.Masters;

/// <summary>Публичное представление мастера с перечнем id оказываемых услуг.</summary>
public record MasterDto(
    Guid Id,
    string Name,
    string? Bio,
    string? PhotoUrl,
    bool IsActive,
    Guid? UserId,
    IReadOnlyList<Guid> ServiceIds);

/// <summary>
/// Создание мастера (admin). Опционально заводит учётную запись мастера
/// (email + начальный пароль) для входа в кабинет.
/// </summary>
public record CreateMasterRequest(
    string Name,
    string? Bio,
    string? PhotoUrl,
    IReadOnlyList<Guid>? ServiceIds,
    string? LoginEmail,
    string? LoginPassword);

/// <summary>
/// Обновление профиля мастера и набора его услуг (admin).
/// Учётка (email/пароль) опциональна:
/// - <see cref="LoginEmail"/> задаёт/меняет email входа (создаёт учётку, если её не было);
/// - <see cref="LoginPassword"/> сбрасывает пароль (если задан).
/// Оба поля null — учётка не трогается.
/// </summary>
public record UpdateMasterRequest(
    string Name,
    string? Bio,
    string? PhotoUrl,
    bool IsActive,
    IReadOnlyList<Guid>? ServiceIds,
    string? LoginEmail = null,
    string? LoginPassword = null);
