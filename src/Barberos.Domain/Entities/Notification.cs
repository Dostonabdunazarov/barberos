using Barberos.Domain.Common;
using Barberos.Domain.Enums;

namespace Barberos.Domain.Entities;

/// <summary>Уведомление в очереди на отправку.</summary>
public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Payload { get; set; } = null!;
    public NotificationStatus Status { get; set; } = NotificationStatus.Queued;
    public DateTime? SentAt { get; set; }
    public DateTime ScheduledFor { get; set; } = DateTime.UtcNow;
    public int Attempts { get; set; }
}
