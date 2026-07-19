using Barberos.Domain.Common;

namespace Barberos.Domain.Entities;

/// <summary>
/// Отзыв клиента по завершённой брони. Один отзыв на бронь.
/// Клиент не является пользователем — отзыв привязан к брони (и через неё к мастеру).
/// </summary>
public class Review : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public Guid MasterId { get; set; }
    public Master Master { get; set; } = null!;

    public int Rating { get; set; } // 1..5
    public string? Comment { get; set; }
    /// <summary>Премодерация: отзыв создаётся скрытым, публикуется после одобрения админом.</summary>
    public bool IsPublished { get; set; } = false;
}
