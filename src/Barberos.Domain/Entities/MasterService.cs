using Barberos.Domain.Common;

namespace Barberos.Domain.Entities;

/// <summary>Связь мастер↔услуга: какие услуги оказывает мастер.</summary>
public class MasterService : BaseEntity
{
    public Guid MasterId { get; set; }
    public Master Master { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}
