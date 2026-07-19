using Barberos.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Barberos.Infrastructure.Auth;

/// <summary>
/// Хеширование паролей поверх ASP.NET Core <see cref="PasswordHasher{TUser}"/> (PBKDF2).
/// Пароль в открытом виде не хранится и не логируется.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    /// <summary>Проверяет пароль; needsRehash=true, если хеш устарел и его стоит обновить.</summary>
    (bool ok, bool needsRehash) Verify(string hash, string password);
}

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public (bool ok, bool needsRehash) Verify(string hash, string password)
    {
        var result = _inner.VerifyHashedPassword(null!, hash, password);
        return result switch
        {
            PasswordVerificationResult.Success => (true, false),
            PasswordVerificationResult.SuccessRehashNeeded => (true, true),
            _ => (false, false)
        };
    }
}
