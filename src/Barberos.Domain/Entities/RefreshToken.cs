using Barberos.Domain.Common;

namespace Barberos.Domain.Entities;

/// <summary>Refresh-токен для продления сессии. Хранится хеш токена.</summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
}
