using Barberos.Domain.Common;

namespace Barberos.Domain.Entities;

/// <summary>Рабочие часы мастера по дню недели.</summary>
public class Schedule : BaseEntity
{
    public Guid MasterId { get; set; }
    public Master Master { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
