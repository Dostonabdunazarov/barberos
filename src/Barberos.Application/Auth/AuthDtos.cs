using Barberos.Domain.Enums;

namespace Barberos.Application.Auth;

/// <summary>Запрос входа сотрудника.</summary>
public record LoginRequest(string Email, string Password);

/// <summary>Запрос смены собственного пароля (авторизован).</summary>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>
/// Результат аутентификации. Access-токен — в теле ответа;
/// refresh-токен персистится и возвращается вызывающему коду для установки в httpOnly cookie.
/// </summary>
public record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    AuthUserDto User);

/// <summary>Публичные данные вошедшего сотрудника.</summary>
public record AuthUserDto(Guid Id, string Email, string? Name, UserRole Role);
