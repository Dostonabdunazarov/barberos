namespace Barberos.Application.Reviews;

/// <summary>
/// Отзыв клиента по завершённой брони (публично, по ManageToken).
/// Мастер и бронь берутся из самой брони — клиент их не задаёт.
/// </summary>
public record CreateReviewRequest(int Rating, string? Comment);

/// <summary>
/// Публичное представление опубликованного отзыва (лента мастера).
/// Имя гостя показываем как есть — иных персональных данных не раскрываем.
/// </summary>
public record ReviewDto(
    Guid Id,
    Guid MasterId,
    string GuestName,
    int Rating,
    string? Comment,
    DateTime CreatedAt);

/// <summary>
/// Представление отзыва для админ-модерации — с флагом публикации и id брони.
/// </summary>
public record ReviewModerationDto(
    Guid Id,
    Guid BookingId,
    Guid MasterId,
    string MasterName,
    string GuestName,
    int Rating,
    string? Comment,
    bool IsPublished,
    DateTime CreatedAt);

/// <summary>Страница списка отзывов для модерации (админ).</summary>
public record ReviewPageDto(
    IReadOnlyList<ReviewModerationDto> Items,
    int Page,
    int PageSize,
    int Total);

/// <summary>
/// Фильтр списка отзывов на модерации. IsPublished=null — все;
/// false — только ожидающие модерации; true — уже опубликованные.
/// </summary>
public record ReviewQuery(
    bool? IsPublished,
    Guid? MasterId,
    int Page = 1,
    int PageSize = 50);

/// <summary>Решение модерации: опубликовать (true) или снять с публикации (false).</summary>
public record ModerateReviewRequest(bool IsPublished);

/// <summary>
/// Агрегированный рейтинг мастера по опубликованным отзывам:
/// среднее и количество. Average=null, если опубликованных отзывов нет.
/// </summary>
public record MasterRatingDto(Guid MasterId, double? Average, int Count);

/// <summary>Публичная лента отзывов мастера с агрегированным рейтингом.</summary>
public record MasterReviewsDto(MasterRatingDto Rating, IReadOnlyList<ReviewDto> Items);
