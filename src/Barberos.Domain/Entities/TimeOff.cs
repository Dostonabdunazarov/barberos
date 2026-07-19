using Barberos.Domain.Common;

namespace Barberos.Domain.Entities;

/// <summary>Период недоступности мастера (отпуск, перерыв). Время в UTC.</summary>
public class TimeOff : BaseEntity
{
    public Guid MasterId { get; set; }
    public Master Master { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Reason { get; set; }
}
