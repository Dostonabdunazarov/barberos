namespace Barberos.Domain.Common;

/// <summary>Базовая сущность с идентификатором и временем создания (UTC).</summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
