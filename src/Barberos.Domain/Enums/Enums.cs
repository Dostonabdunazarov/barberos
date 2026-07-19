namespace Barberos.Domain.Enums;

/// <summary>Роль пользователя в системе.</summary>
public enum UserRole
{
    Client = 0,
    Master = 1,
    Admin = 2
}

/// <summary>Статус брони.</summary>
public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3,
    NoShow = 4
}

/// <summary>Тип уведомления.</summary>
public enum NotificationType
{
    BookingCreated = 0,
    Reminder = 1,
    Cancelled = 2,
    Rescheduled = 3,
    ReviewRequest = 4
}

/// <summary>Канал доставки уведомления.</summary>
public enum NotificationChannel
{
    Sms = 0,
    Email = 1,
    Telegram = 2
}

/// <summary>Статус уведомления в очереди.</summary>
public enum NotificationStatus
{
    Queued = 0,
    Sent = 1,
    Failed = 2
}
