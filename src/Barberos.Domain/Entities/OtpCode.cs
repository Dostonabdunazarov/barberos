using Barberos.Domain.Common;

namespace Barberos.Domain.Entities;

/// <summary>Одноразовый код подтверждения для входа по SMS. Хранится хеш кода.</summary>
public class OtpCode : BaseEntity
{
    public string Phone { get; set; } = null!;
    public string CodeHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public bool Used { get; set; }
}
