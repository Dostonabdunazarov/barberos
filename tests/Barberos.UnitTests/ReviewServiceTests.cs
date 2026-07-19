using Barberos.Application.Common;
using Barberos.Application.Reviews;
using Barberos.Domain.Entities;
using Barberos.Domain.Enums;
using Barberos.Infrastructure.Persistence;
using Barberos.Infrastructure.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Barberos.UnitTests;

/// <summary>
/// Тесты отзывов (PLAN.md §Этап 4). Премодерация: отзыв создаётся скрытым,
/// публикуется только после одобрения админом. Один отзыв на бронь.
/// </summary>
public class ReviewServiceTests
{
    private static readonly Guid MasterId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherMasterId = Guid.Parse("1a1a1a1a-1111-1111-1111-111111111111");
    private static readonly Guid ServiceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static void SeedMasters(AppDbContext db)
    {
        db.Masters.Add(new Master { Id = MasterId, Name = "Тест", IsActive = true });
        db.Masters.Add(new Master { Id = OtherMasterId, Name = "Другой", IsActive = true });
        db.Services.Add(new Service { Id = ServiceId, Name = "Стрижка", DurationMinutes = 30, IsActive = true });
    }

    /// <summary>Создаёт бронь с заданным статусом и возвращает её ManageToken.</summary>
    private static async Task<Guid> SeedBookingAsync(
        AppDbContext db, BookingStatus status, Guid? masterId = null, string guestName = "Иван")
    {
        var booking = new Booking
        {
            MasterId = masterId ?? MasterId,
            ServiceId = ServiceId,
            GuestName = guestName,
            GuestPhone = "+998901234567",
            StartAt = new DateTime(2030, 1, 7, 5, 0, 0, DateTimeKind.Utc),
            EndAt = new DateTime(2030, 1, 7, 5, 30, 0, DateTimeKind.Utc),
            Status = status,
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking.ManageToken;
    }

    // ── Создание ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_OnCompletedBooking_PersistsUnpublishedReview()
    {
        using var db = NewDb();
        SeedMasters(db);
        var token = await SeedBookingAsync(db, BookingStatus.Completed);

        var dto = await new ReviewService(db).CreateByManageTokenAsync(
            token, new CreateReviewRequest(5, "  Отлично  "));

        Assert.Equal(5, dto.Rating);
        Assert.Equal("Отлично", dto.Comment); // trim
        Assert.Equal("Иван", dto.GuestName);

        var saved = await db.Reviews.SingleAsync();
        Assert.False(saved.IsPublished); // премодерация
        Assert.Equal(MasterId, saved.MasterId);
    }

    [Fact]
    public async Task Create_BlankComment_StoredAsNull()
    {
        using var db = NewDb();
        SeedMasters(db);
        var token = await SeedBookingAsync(db, BookingStatus.Completed);

        var dto = await new ReviewService(db).CreateByManageTokenAsync(
            token, new CreateReviewRequest(4, "   "));

        Assert.Null(dto.Comment);
    }

    [Fact]
    public async Task Create_UnknownToken_ThrowsNotFound()
    {
        using var db = NewDb();
        SeedMasters(db);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ReviewService(db).CreateByManageTokenAsync(Guid.NewGuid(), new CreateReviewRequest(5, null)));
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.NoShow)]
    public async Task Create_OnNonCompletedBooking_ThrowsConflict(BookingStatus status)
    {
        using var db = NewDb();
        SeedMasters(db);
        var token = await SeedBookingAsync(db, status);

        await Assert.ThrowsAsync<ConflictException>(() =>
            new ReviewService(db).CreateByManageTokenAsync(token, new CreateReviewRequest(5, null)));
    }

    [Fact]
    public async Task Create_Twice_ThrowsConflict()
    {
        using var db = NewDb();
        SeedMasters(db);
        var token = await SeedBookingAsync(db, BookingStatus.Completed);
        await new ReviewService(db).CreateByManageTokenAsync(token, new CreateReviewRequest(5, null));

        await Assert.ThrowsAsync<ConflictException>(() =>
            new ReviewService(db).CreateByManageTokenAsync(token, new CreateReviewRequest(4, null)));
    }

    // ── Публичная лента мастера ──────────────────────────────────────────────────

    [Fact]
    public async Task GetMasterReviews_ReturnsOnlyPublished_WithAverage()
    {
        using var db = NewDb();
        SeedMasters(db);
        var t1 = await SeedBookingAsync(db, BookingStatus.Completed, guestName: "A");
        var t2 = await SeedBookingAsync(db, BookingStatus.Completed, guestName: "B");
        var svc = new ReviewService(db);
        var r1 = await svc.CreateByManageTokenAsync(t1, new CreateReviewRequest(5, "top"));
        await svc.CreateByManageTokenAsync(t2, new CreateReviewRequest(3, "meh"));

        // Публикуем только первый (5).
        await svc.ModerateAsync(r1.Id, new ModerateReviewRequest(true));

        var feed = await svc.GetMasterReviewsAsync(MasterId);

        Assert.Single(feed.Items);
        Assert.Equal(5, feed.Items[0].Rating);
        Assert.Equal(1, feed.Rating.Count);
        Assert.Equal(5.0, feed.Rating.Average);
    }

    [Fact]
    public async Task GetMasterReviews_NoPublished_AverageIsNull()
    {
        using var db = NewDb();
        SeedMasters(db);
        var token = await SeedBookingAsync(db, BookingStatus.Completed);
        await new ReviewService(db).CreateByManageTokenAsync(token, new CreateReviewRequest(5, null));

        var feed = await new ReviewService(db).GetMasterReviewsAsync(MasterId);

        Assert.Empty(feed.Items);
        Assert.Equal(0, feed.Rating.Count);
        Assert.Null(feed.Rating.Average);
    }

    [Fact]
    public async Task GetMasterReviews_UnknownMaster_ThrowsNotFound()
    {
        using var db = NewDb();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ReviewService(db).GetMasterReviewsAsync(Guid.NewGuid()));
    }

    // ── Модерация ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Moderate_PublishThenUnpublish_TogglesVisibility()
    {
        using var db = NewDb();
        SeedMasters(db);
        var token = await SeedBookingAsync(db, BookingStatus.Completed);
        var svc = new ReviewService(db);
        var created = await svc.CreateByManageTokenAsync(token, new CreateReviewRequest(5, "ok"));

        var published = await svc.ModerateAsync(created.Id, new ModerateReviewRequest(true));
        Assert.True(published.IsPublished);
        Assert.Equal("Тест", published.MasterName);
        Assert.Single((await svc.GetMasterReviewsAsync(MasterId)).Items);

        var hidden = await svc.ModerateAsync(created.Id, new ModerateReviewRequest(false));
        Assert.False(hidden.IsPublished);
        Assert.Empty((await svc.GetMasterReviewsAsync(MasterId)).Items);
    }

    [Fact]
    public async Task Moderate_UnknownReview_ThrowsNotFound()
    {
        using var db = NewDb();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ReviewService(db).ModerateAsync(Guid.NewGuid(), new ModerateReviewRequest(true)));
    }

    [Fact]
    public async Task ListForModeration_FiltersUnpublishedFirst()
    {
        using var db = NewDb();
        SeedMasters(db);
        var t1 = await SeedBookingAsync(db, BookingStatus.Completed, guestName: "A");
        var t2 = await SeedBookingAsync(db, BookingStatus.Completed, guestName: "B");
        var svc = new ReviewService(db);
        var r1 = await svc.CreateByManageTokenAsync(t1, new CreateReviewRequest(5, null));
        await svc.CreateByManageTokenAsync(t2, new CreateReviewRequest(4, null));
        await svc.ModerateAsync(r1.Id, new ModerateReviewRequest(true));

        var pending = await svc.ListForModerationAsync(new ReviewQuery(IsPublished: false, null));
        Assert.Single(pending.Items);
        Assert.False(pending.Items[0].IsPublished);

        var all = await svc.ListForModerationAsync(new ReviewQuery(null, null));
        Assert.Equal(2, all.Total);
        // Не опубликованные — сверху.
        Assert.False(all.Items[0].IsPublished);
    }

    [Fact]
    public async Task ListForModeration_FiltersByMaster()
    {
        using var db = NewDb();
        SeedMasters(db);
        var t1 = await SeedBookingAsync(db, BookingStatus.Completed, masterId: MasterId);
        var t2 = await SeedBookingAsync(db, BookingStatus.Completed, masterId: OtherMasterId);
        var svc = new ReviewService(db);
        await svc.CreateByManageTokenAsync(t1, new CreateReviewRequest(5, null));
        await svc.CreateByManageTokenAsync(t2, new CreateReviewRequest(4, null));

        var page = await svc.ListForModerationAsync(new ReviewQuery(null, MasterId));

        Assert.Single(page.Items);
        Assert.Equal(MasterId, page.Items[0].MasterId);
    }
}
