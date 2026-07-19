namespace Barberos.Application.Reviews;

/// <summary>
/// Отзывы клиентов и их модерация. Создание — публично по ManageToken завершённой брони;
/// публичная лента мастера — только опубликованные; список для модерации и решение —
/// только admin (проверяется policy на уровне API). Реализация — в Infrastructure.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Создаёт отзыв по ManageToken завершённой брони. Отзыв создаётся скрытым
    /// (премодерация). Бросает NotFoundException, если токен неизвестен;
    /// ConflictException, если бронь не завершена или отзыв уже оставлен.
    /// </summary>
    Task<ReviewDto> CreateByManageTokenAsync(
        Guid manageToken, CreateReviewRequest request, CancellationToken ct = default);

    /// <summary>Публичная лента мастера: опубликованные отзывы + агрегированный рейтинг.</summary>
    Task<MasterReviewsDto> GetMasterReviewsAsync(Guid masterId, CancellationToken ct = default);

    /// <summary>Список отзывов для модерации с фильтрами и пагинацией (admin).</summary>
    Task<ReviewPageDto> ListForModerationAsync(ReviewQuery query, CancellationToken ct = default);

    /// <summary>Публикация/снятие с публикации отзыва (admin). NotFound, если отзыв не найден.</summary>
    Task<ReviewModerationDto> ModerateAsync(
        Guid id, ModerateReviewRequest request, CancellationToken ct = default);
}
