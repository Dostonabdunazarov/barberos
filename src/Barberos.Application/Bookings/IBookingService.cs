namespace Barberos.Application.Bookings;

/// <summary>
/// Контекст вошедшего сотрудника для операций над бронями.
/// IsAdmin=true — полный доступ; иначе доступ ограничен бронями своего мастера.
/// </summary>
public record StaffContext(Guid UserId, bool IsAdmin);

/// <summary>
/// Операции над бронями. Публичное создание/просмотр — без авторизации;
/// список/статусы/отмена/перенос — только персонал (см. StaffContext).
/// Реализация — в Infrastructure (транзакции + защита от двойного бронирования).
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Публичное создание гостевой брони. Проверяет мастера/услугу/связь, попадание
    /// слота в расписание, lead time; создаёт бронь в транзакции с защитой от двойного
    /// бронирования (EXCLUDE-constraint). Бросает NotFoundException/ConflictException.
    /// </summary>
    Task<CreateBookingResult> CreateAsync(CreateBookingRequest request, CancellationToken ct = default);

    /// <summary>Просмотр брони клиентом по ManageToken (только чтение). NotFound, если токен неизвестен.</summary>
    Task<BookingManageDto> GetByManageTokenAsync(Guid manageToken, CancellationToken ct = default);

    /// <summary>Список броней для персонала с фильтрами и пагинацией (мастер видит только свои).</summary>
    Task<BookingPageDto> ListAsync(BookingQuery query, StaffContext caller, CancellationToken ct = default);

    /// <summary>Одна бронь для персонала. Forbidden, если чужая (для не-админа); NotFound, если нет.</summary>
    Task<BookingDto> GetAsync(Guid id, StaffContext caller, CancellationToken ct = default);

    /// <summary>
    /// Смена статуса брони (completed / no_show / cancelled). Проверяет допустимость перехода.
    /// Forbidden, если чужая; NotFound, если нет; Conflict при недопустимом переходе.
    /// </summary>
    Task<BookingDto> UpdateStatusAsync(
        Guid id, UpdateBookingStatusRequest request, StaffContext caller, CancellationToken ct = default);

    /// <summary>
    /// Перенос брони (транзакция + повторная проверка слота под EXCLUDE-constraint).
    /// Только для активной (confirmed) брони. Forbidden/NotFound/Conflict по ситуации.
    /// </summary>
    Task<BookingDto> RescheduleAsync(
        Guid id, RescheduleBookingRequest request, StaffContext caller, CancellationToken ct = default);
}
