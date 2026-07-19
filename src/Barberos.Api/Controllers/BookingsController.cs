using Barberos.Api.Auth;
using Barberos.Api.RateLimiting;
using Barberos.Application.Bookings;
using Barberos.Application.Common;
using Barberos.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Barberos.Api.Controllers;

/// <summary>
/// Брони. Создание и просмотр по ManageToken — публично; список, смена статуса,
/// отмена и перенос — только персонал (мастер видит/меняет лишь свои брони, админ — все).
/// </summary>
[ApiController]
[Route("api/bookings")]
public class BookingsController(IBookingService bookings) : ControllerBase
{
    // ── Публичные ────────────────────────────────────────────────────────────────

    /// <summary>Создание гостевой брони. Возвращает id и секретный manageToken.</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitSetup.BookingPolicy)]
    public async Task<ActionResult<CreateBookingResult>> Create(CreateBookingRequest request, CancellationToken ct)
    {
        var result = await bookings.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetByToken), new { token = result.ManageToken }, result);
    }

    /// <summary>Просмотр своей брони клиентом по manageToken (только чтение).</summary>
    [HttpGet("manage/{token:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<BookingManageDto>> GetByToken(Guid token, CancellationToken ct)
        => Ok(await bookings.GetByManageTokenAsync(token, ct));

    // ── Персонал ─────────────────────────────────────────────────────────────────

    /// <summary>Список броней с фильтрами и пагинацией. Мастер видит только свои.</summary>
    [HttpGet]
    [Authorize(Policy = AuthSetup.StaffPolicy)]
    public async Task<ActionResult<BookingPageDto>> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? masterId,
        [FromQuery] BookingStatus? status,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken ct)
    {
        var query = new BookingQuery(from, to, masterId, status,
            page <= 0 ? 1 : page,
            pageSize <= 0 ? 50 : pageSize);
        return Ok(await bookings.ListAsync(query, Caller(), ct));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthSetup.StaffPolicy)]
    public async Task<ActionResult<BookingDto>> Get(Guid id, CancellationToken ct)
        => Ok(await bookings.GetAsync(id, Caller(), ct));

    /// <summary>Смена статуса: confirmed → completed / no_show / cancelled.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = AuthSetup.StaffPolicy)]
    public async Task<ActionResult<BookingDto>> UpdateStatus(
        Guid id, UpdateBookingStatusRequest request, CancellationToken ct)
        => Ok(await bookings.UpdateStatusAsync(id, request, Caller(), ct));

    /// <summary>Отмена брони по обращению клиента (переводит в cancelled).</summary>
    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Policy = AuthSetup.StaffPolicy)]
    public async Task<ActionResult<BookingDto>> Cancel(Guid id, CancellationToken ct)
        => Ok(await bookings.UpdateStatusAsync(
            id, new UpdateBookingStatusRequest(BookingStatus.Cancelled), Caller(), ct));

    /// <summary>Перенос брони: транзакция + повторная проверка слота.</summary>
    [HttpPatch("{id:guid}/reschedule")]
    [Authorize(Policy = AuthSetup.StaffPolicy)]
    public async Task<ActionResult<BookingDto>> Reschedule(
        Guid id, RescheduleBookingRequest request, CancellationToken ct)
        => Ok(await bookings.RescheduleAsync(id, request, Caller(), ct));

    /// <summary>Контекст вошедшего сотрудника из claims (endpoint защищён StaffPolicy).</summary>
    private StaffContext Caller()
    {
        var userId = User.GetUserId()
            ?? throw new ForbiddenException("Не удалось определить пользователя.");
        return new StaffContext(userId, User.IsAdmin());
    }
}
