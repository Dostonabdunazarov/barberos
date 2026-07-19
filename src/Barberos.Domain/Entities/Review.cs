using Barberos.Domain.Common;

namespace Barberos.Domain.Entities;

/// <summary>Отзыв клиента по завершённой брони. Один отзыв на бронь.</summary>
public class Review : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public Guid ClientId { get; set; }
    public User Client { get; set; } = null!;
    public Guid MasterId { get; set; }
    public Master Master { get; set; } = null!;

    public int Rating { get; set; } // 1..5
    public string? Comment { get; set; }
    public bool IsPublished { get; set; } = true;
}
