namespace Barberos.Domain.Enums;

/// <summary>Роль сотрудника. Клиенты не являются пользователями системы.</summary>
public enum UserRole
{
    Master = 1,
    Admin = 2
}

/// <summary>Статус брони. Гостевая бронь создаётся сразу как Confirmed.</summary>
public enum BookingStatus
{
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3,
    NoShow = 4
}
