using Barberos.Domain.Common;
using Barberos.Domain.Enums;

namespace Barberos.Domain.Entities;

/// <summary>Пользователь системы (клиент, мастер или админ). Идентификация по телефону.</summary>
public class User : BaseEntity
{
    public string Phone { get; set; } = null!;
    public string? Name { get; set; }
    public UserRole Role { get; set; } = UserRole.Client;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
