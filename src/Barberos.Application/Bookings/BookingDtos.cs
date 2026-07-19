using Barberos.Domain.Enums;

namespace Barberos.Application.Bookings;

/// <summary>
/// Публичное создание гостевой брони. StartAt — в UTC (ISO 8601 с Z).
/// EndAt рассчитывается сервером (duration + buffer услуги) — клиент его не задаёт.
/// </summary>
public record CreateBookingRequest(
    string GuestName,
    string GuestPhone,
    Guid MasterId,
    Guid ServiceId,
    DateTime StartAt);

/// <summary>
/// Результат создания брони: id и секретный ManageToken.
/// ManageToken возвращается клиенту один раз — по нему он смотрит бронь и оставляет отзыв.
/// </summary>
public record CreateBookingResult(Guid Id, Guid ManageToken);

/// <summary>
/// Представление брони для клиента (по ManageToken) — только чтение.
/// Телефон гостя не возвращается (он и так его знает; лишний раз секрет не светим).
/// </summary>
public record BookingManageDto(
    Guid Id,
    string GuestName,
    string MasterName,
    string ServiceName,
    DateTime StartAt,
    DateTime EndAt,
    decimal Price,
    BookingStatus Status);

/// <summary>Представление брони для персонала (мастер/админ) — с контактами гостя.</summary>
public record BookingDto(
    Guid Id,
    string GuestName,
    string GuestPhone,
    Guid MasterId,
    string MasterName,
    Guid ServiceId,
    string ServiceName,
    DateTime StartAt,
    DateTime EndAt,
    BookingStatus Status,
    DateTime CreatedAt);

/// <summary>Страница списка броней (персонал).</summary>
public record BookingPageDto(
    IReadOnlyList<BookingDto> Items,
    int Page,
    int PageSize,
    int Total);

/// <summary>
/// Фильтр списка броней персонала. Все поля опциональны.
/// MasterId=null — все мастера (доступно только админу; мастер видит только свои).
/// From/To — границы по StartAt (UTC).
/// </summary>
public record BookingQuery(
    DateTime? From,
    DateTime? To,
    Guid? MasterId,
    BookingStatus? Status,
    int Page = 1,
    int PageSize = 50);

/// <summary>
/// Смена статуса брони персоналом: confirmed → completed / no_show / cancelled.
/// </summary>
public record UpdateBookingStatusRequest(BookingStatus Status);

/// <summary>
/// Перенос брони персоналом: новый старт (UTC). EndAt пересчитывается по услуге.
/// Услугу/мастера можно оставить прежними (null) или сменить.
/// </summary>
public record RescheduleBookingRequest(
    DateTime NewStartAt,
    Guid? NewMasterId,
    Guid? NewServiceId);
