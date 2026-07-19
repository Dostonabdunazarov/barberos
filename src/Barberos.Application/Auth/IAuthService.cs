namespace Barberos.Application.Auth;

/// <summary>
/// Аутентификация персонала (мастер/админ). Клиенты не аутентифицируются.
/// Реализация — в Infrastructure.
/// </summary>
public interface IAuthService
{
    /// <summary>Вход по email + паролю. Бросает <see cref="AuthException"/> при неверных данных.</summary>
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>Обновление пары токенов по действующему refresh-токену (ротация).</summary>
    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Отзыв refresh-токена (logout). Идемпотентно.</summary>
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Смена собственного пароля вошедшим сотрудником.</summary>
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}

/// <summary>Ошибка аутентификации/авторизации; маппится в 401 на уровне API.</summary>
public class AuthException(string message) : Exception(message);
