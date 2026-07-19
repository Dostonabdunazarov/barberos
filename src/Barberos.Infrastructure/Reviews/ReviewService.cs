using Barberos.Application.Abstractions;
using Barberos.Application.Common;
using Barberos.Application.Reviews;
using Barberos.Domain.Entities;
using Barberos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Barberos.Infrastructure.Reviews;

/// <summary>
/// Отзывы поверх EF Core. Один отзыв на бронь (unique-индекс BookingId),
/// создаётся скрытым (премодерация) по завершённой брони; админ публикует/снимает.
/// </summary>
public sealed class ReviewService(IAppDbContext db) : IReviewService
{
    // ── Создание клиентом по токену (публично) ──────────────────────────────────

    public async Task<ReviewDto> CreateByManageTokenAsync(
        Guid manageToken, CreateReviewRequest request, CancellationToken ct = default)
    {
        var booking = await db.Bookings.AsNoTracking()
            .Select(b => new { b.Id, b.ManageToken, b.MasterId, b.GuestName, b.Status })
            .FirstOrDefaultAsync(b => b.ManageToken == manageToken, ct)
            ?? throw new NotFoundException("Бронь не найдена.");

        if (booking.Status != BookingStatus.Completed)
            throw new ConflictException("Отзыв можно оставить только после завершённого визита.");

        var alreadyReviewed = await db.Reviews.AsNoTracking()
            .AnyAsync(r => r.BookingId == booking.Id, ct);
        if (alreadyReviewed)
            throw new ConflictException("Отзыв по этой брони уже оставлен.");

        var review = new Review
        {
            BookingId = booking.Id,
            MasterId = booking.MasterId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            IsPublished = false, // премодерация
        };

        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);

        return new ReviewDto(
            review.Id, review.MasterId, booking.GuestName, review.Rating, review.Comment, review.CreatedAt);
    }

    // ── Публичная лента мастера ──────────────────────────────────────────────────

    public async Task<MasterReviewsDto> GetMasterReviewsAsync(Guid masterId, CancellationToken ct = default)
    {
        var masterExists = await db.Masters.AsNoTracking().AnyAsync(m => m.Id == masterId, ct);
        if (!masterExists)
            throw new NotFoundException("Мастер не найден.");

        var published = db.Reviews.AsNoTracking()
            .Where(r => r.MasterId == masterId && r.IsPublished);

        var count = await published.CountAsync(ct);
        var average = count == 0 ? (double?)null : await published.AverageAsync(r => (double)r.Rating, ct);

        var items = await published
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                r.Id,
                r.MasterId,
                r.Booking.GuestName,
                r.Rating,
                r.Comment,
                r.CreatedAt))
            .ToListAsync(ct);

        return new MasterReviewsDto(new MasterRatingDto(masterId, average, count), items);
    }

    // ── Список для модерации (admin) ─────────────────────────────────────────────

    public async Task<ReviewPageDto> ListForModerationAsync(ReviewQuery query, CancellationToken ct = default)
    {
        var q = db.Reviews.AsNoTracking().AsQueryable();

        if (query.IsPublished is { } isPublished)
            q = q.Where(r => r.IsPublished == isPublished);
        if (query.MasterId is { } masterId)
            q = q.Where(r => r.MasterId == masterId);

        var total = await q.CountAsync(ct);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var items = await q
            // Сначала ожидающие модерации (не опубликованные), затем свежие.
            .OrderBy(r => r.IsPublished)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReviewModerationDto(
                r.Id,
                r.BookingId,
                r.MasterId,
                r.Master.Name,
                r.Booking.GuestName,
                r.Rating,
                r.Comment,
                r.IsPublished,
                r.CreatedAt))
            .ToListAsync(ct);

        return new ReviewPageDto(items, page, pageSize, total);
    }

    // ── Модерация (admin) ────────────────────────────────────────────────────────

    public async Task<ReviewModerationDto> ModerateAsync(
        Guid id, ModerateReviewRequest request, CancellationToken ct = default)
    {
        var review = await db.Reviews
            .Include(r => r.Master)
            .Include(r => r.Booking)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Отзыв не найден.");

        review.IsPublished = request.IsPublished;
        await db.SaveChangesAsync(ct);

        return new ReviewModerationDto(
            review.Id,
            review.BookingId,
            review.MasterId,
            review.Master.Name,
            review.Booking.GuestName,
            review.Rating,
            review.Comment,
            review.IsPublished,
            review.CreatedAt);
    }
}
