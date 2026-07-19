using Barberos.Domain.Common;
using Barberos.Domain.Enums;

namespace Barberos.Domain.Entities;

/// <summary>Бронь: запись клиента к мастеру на услугу. Время в UTC.</summary>
public class Booking : BaseEntity
{
    public Guid ClientId { get; set; }
    public User Client { get; set; } = null!;
    public Guid MasterId { get; set; }
    public Master Master { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public Review? Review { get; set; }
}
