using Barberos.Domain.Common;

namespace Barberos.Domain.Entities;

/// <summary>Мастер (барбер). Может быть связан с учётной записью User.</summary>
public class Master : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = null!;
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MasterService> MasterServices { get; set; } = new List<MasterService>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    public ICollection<TimeOff> TimeOffs { get; set; } = new List<TimeOff>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
