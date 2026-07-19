namespace Barberos.Application.Common;

/// <summary>Запрошенный ресурс не найден; маппится в 404 на уровне API.</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>Действие запрещено для текущего пользователя; маппится в 403 на уровне API.</summary>
public sealed class ForbiddenException(string message) : Exception(message);

/// <summary>
/// Конфликт состояния (нарушение бизнес-правила или уникальности);
/// маппится в 409 на уровне API.
/// </summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>
/// Ошибка валидации входных данных; маппится в 400 (ProblemDetails с ошибками по полям).
/// </summary>
public sealed class ValidationAppException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Ошибка валидации входных данных.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
