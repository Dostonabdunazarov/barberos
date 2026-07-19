using Barberos.Domain.Common;
using Barberos.Domain.Enums;

namespace Barberos.Domain.Entities;

/// <summary>
/// Бронь: гостевая запись клиента к мастеру на услугу. Время в UTC.
/// Клиент не имеет учётной записи — его данные (имя, телефон) хранятся здесь.
/// Доступ клиента к своей брони (отмена/перенос/отзыв) — по секретному ManageToken.
/// </summary>
public class Booking : BaseEntity
{
    public string GuestName { get; set; } = null!;
    public string GuestPhone { get; set; } = null!;
    /// <summary>Секретный токен для управления бронью клиентом без входа.</summary>
    public Guid ManageToken { get; set; } = Guid.NewGuid();

    public Guid MasterId { get; set; }
    public Master Master { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public Review? Review { get; set; }
}
