using Barberos.Domain.Common;
using Barberos.Domain.Enums;

namespace Barberos.Domain.Entities;

/// <summary>
/// Бронь: гостевая запись клиента к мастеру на услугу. Время в UTC.
/// Клиент не имеет учётной записи — его данные (имя, телефон) хранятся здесь.
/// Доступ клиента к своей брони (просмотр + отзыв) — по секретному ManageToken.
/// Отмену/перенос делает только персонал.
/// </summary>
public class Booking : BaseEntity
{
    public string GuestName { get; set; } = null!;
    public string GuestPhone { get; set; } = null!;
    /// <summary>
    /// Секретный токен-возможность (capability) для просмотра брони и отзыва клиентом без входа.
    /// GUID v4 из <see cref="Guid.NewGuid"/> на .NET Core+ формируется через криптографический
    /// генератор ОС (122 бита энтропии), поэтому непредсказуем и пригоден как секрет.
    /// Обращаться как с секретом: передавать только по HTTPS, не логировать в открытом виде.
    /// </summary>
    public Guid ManageToken { get; set; } = Guid.NewGuid();

    public Guid MasterId { get; set; }
    public Master Master { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public DateTime StartAt { get; set; }
    /// <summary>Конец интервала занятости мастера, включает буфер услуги.</summary>
    public DateTime EndAt { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    public Review? Review { get; set; }
}
