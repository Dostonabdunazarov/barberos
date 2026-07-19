using Barberos.Domain.Common;
using Barberos.Domain.Enums;

namespace Barberos.Domain.Entities;

/// <summary>
/// Уведомление в очереди на отправку. Адресат — телефон/email (клиент по гостевым
/// данным брони или сотрудник), а не ссылка на User, т.к. клиенты не пользователи.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>Телефон или email адресата.</summary>
    public string Recipient { get; set; } = null!;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Payload { get; set; } = null!;
    public NotificationStatus Status { get; set; } = NotificationStatus.Queued;
    public DateTime ScheduledFor { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public int Attempts { get; set; }
}
