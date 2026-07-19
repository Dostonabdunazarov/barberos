using Barberos.Api.Auth;
using Barberos.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barberos.Api.Controllers;

/// <summary>
/// Отзывы. Создание — публично по ManageToken завершённой брони (премодерация);
/// список на модерацию и решение — только admin. Публичная лента мастера —
/// в <see cref="MastersController"/> (GET /api/masters/{id}/reviews).
/// </summary>
[ApiController]
[Route("api/reviews")]
public class ReviewsController(IReviewService reviews) : ControllerBase
{
    /// <summary>Оставить отзыв по manageToken завершённой брони. Отзыв уходит на модерацию.</summary>
    [HttpPost("manage/{token:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ReviewDto>> Create(
        Guid token, CreateReviewRequest request, CancellationToken ct)
        => Ok(await reviews.CreateByManageTokenAsync(token, request, ct));

    /// <summary>Список отзывов для модерации (admin). По умолчанию — ожидающие модерации сверху.</summary>
    [HttpGet]
    [Authorize(Policy = AuthSetup.AdminPolicy)]
    public async Task<ActionResult<ReviewPageDto>> List(
        [FromQuery] bool? isPublished,
        [FromQuery] Guid? masterId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken ct)
    {
        var query = new ReviewQuery(isPublished, masterId,
            page <= 0 ? 1 : page,
            pageSize <= 0 ? 50 : pageSize);
        return Ok(await reviews.ListForModerationAsync(query, ct));
    }

    /// <summary>Опубликовать / снять с публикации отзыв (admin).</summary>
    [HttpPatch("{id:guid}/moderate")]
    [Authorize(Policy = AuthSetup.AdminPolicy)]
    public async Task<ActionResult<ReviewModerationDto>> Moderate(
        Guid id, ModerateReviewRequest request, CancellationToken ct)
        => Ok(await reviews.ModerateAsync(id, request, ct));
}
