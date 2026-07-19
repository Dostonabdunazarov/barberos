using Barberos.Application.Abstractions;
using Barberos.Application.Auth;
using Barberos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barberos.Infrastructure.Auth;

/// <summary>
/// Аутентификация персонала: вход, ротация refresh-токенов, logout, смена пароля.
/// Refresh-токены хранятся хешированными; при обновлении старый отзывается (ротация).
/// </summary>
public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwt) : IAuthService
{
    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Одинаковый ответ на «нет пользователя» и «неверный пароль» — не раскрываем наличие email.
        if (user is null || !user.IsActive)
            throw new AuthException("Неверный email или пароль.");

        var (ok, needsRehash) = passwordHasher.Verify(user.PasswordHash, request.Password);
        if (!ok)
            throw new AuthException("Неверный email или пароль.");

        if (needsRehash)
            user.PasswordHash = passwordHasher.Hash(request.Password);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new AuthException("Refresh-токен отсутствует.");

        var hash = jwt.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

        if (stored is null || stored.Revoked || stored.ExpiresAt <= DateTime.UtcNow)
            throw new AuthException("Refresh-токен недействителен.");
        if (!stored.User.IsActive)
            throw new AuthException("Учётная запись отключена.");

        // Ротация: отзываем предъявленный токен, выдаём новую пару.
        stored.Revoked = true;
        return await IssueTokensAsync(stored.User, ct);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hash = jwt.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);
        if (stored is not null && !stored.Revoked)
        {
            stored.Revoked = true;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new AuthException("Пользователь не найден.");

        var (ok, _) = passwordHasher.Verify(user.PasswordHash, request.CurrentPassword);
        if (!ok)
            throw new AuthException("Текущий пароль неверен.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        // Меняешь пароль — все refresh-сессии отзываются.
        await db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.Revoked)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.Revoked, true), ct);

        await db.SaveChangesAsync(ct);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (accessToken, accessExpires) = jwt.CreateAccessToken(user);
        var (rawRefresh, refreshHash, refreshExpires) = jwt.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpires,
        });
        await db.SaveChangesAsync(ct);

        return new AuthResult(
            accessToken,
            accessExpires,
            rawRefresh,
            refreshExpires,
            new AuthUserDto(user.Id, user.Email, user.Name, user.Role));
    }
}
