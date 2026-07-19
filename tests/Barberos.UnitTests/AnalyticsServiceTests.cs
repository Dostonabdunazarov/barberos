using Barberos.Application.Analytics;
using Barberos.Domain.Entities;
using Barberos.Domain.Enums;
using Barberos.Infrastructure.Analytics;
using Barberos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Barberos.UnitTests;

/// <summary>
/// Тесты базовой админ-аналитики (PLAN.md §Этап 4): брони по статусам,
/// загрузка мастеров, популярные услуги. Занятыми считаются Confirmed/Completed.
/// </summary>
public class AnalyticsServiceTests
{
    private static readonly Guid M1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid M2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid S1 = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    private static readonly Guid S2 = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222");

    private static readonly DateTime Day = new(2030, 1, 7, 5, 0, 0, DateTimeKind.Utc);

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static void SeedRefs(AppDbContext db)
    {
        db.Masters.Add(new Master { Id = M1, Name = "Али", IsActive = true });
        db.Masters.Add(new Master { Id = M2, Name = "Бек", IsActive = true });
        db.Services.Add(new Service { Id = S1, Name = "Стрижка", DurationMinutes = 30, IsActive = true });
        db.Services.Add(new Service { Id = S2, Name = "Борода", DurationMinutes = 20, IsActive = true });
    }

    private static Booking Bk(Guid masterId, Guid serviceId, BookingStatus status, DateTime start, int minutes) =>
        new()
        {
            MasterId = masterId,
            ServiceId = serviceId,
            GuestName = "Гость",
            GuestPhone = "+998900000000",
            StartAt = start,
            EndAt = start.AddMinutes(minutes),
            Status = status,
        };

    [Fact]
    public async Task Overview_CountsByStatus_AcrossAllStatuses()
    {
        using var db = NewDb();
        SeedRefs(db);
        db.Bookings.AddRange(
            Bk(M1, S1, BookingStatus.Completed, Day, 30),
            Bk(M1, S1, BookingStatus.Confirmed, Day.AddHours(1), 30),
            Bk(M1, S1, BookingStatus.Cancelled, Day.AddHours(2), 30),
            Bk(M1, S1, BookingStatus.NoShow, Day.AddHours(3), 30));
        await db.SaveChangesAsync();

        var overview = await new AnalyticsService(db).GetOverviewAsync(new AnalyticsQuery(null, null));

        Assert.Equal(4, overview.TotalBookings);
        Assert.Equal(1, overview.ByStatus.Single(s => s.Status == BookingStatus.Completed).Count);
        Assert.Equal(1, overview.ByStatus.Single(s => s.Status == BookingStatus.Cancelled).Count);
        Assert.Equal(1, overview.ByStatus.Single(s => s.Status == BookingStatus.NoShow).Count);
    }

    [Fact]
    public async Task Overview_MasterLoad_CountsBusyBookingsAndMinutes()
    {
        using var db = NewDb();
        SeedRefs(db);
        db.Bookings.AddRange(
            Bk(M1, S1, BookingStatus.Completed, Day, 30),
            Bk(M1, S1, BookingStatus.Confirmed, Day.AddHours(1), 45),
            // Отменённые/неявка не занимают время — не в загрузке.
            Bk(M1, S1, BookingStatus.Cancelled, Day.AddHours(2), 30),
            Bk(M2, S2, BookingStatus.Completed, Day, 20));
        await db.SaveChangesAsync();

        var overview = await new AnalyticsService(db).GetOverviewAsync(new AnalyticsQuery(null, null));

        var m1 = overview.MasterLoad.Single(m => m.MasterId == M1);
        Assert.Equal(2, m1.Bookings);
        Assert.Equal(75, m1.BusyMinutes); // 30 + 45

        var m2 = overview.MasterLoad.Single(m => m.MasterId == M2);
        Assert.Equal(1, m2.Bookings);
        Assert.Equal(20, m2.BusyMinutes);

        // Отсортировано по числу броней убыв. — M1 первым.
        Assert.Equal(M1, overview.MasterLoad[0].MasterId);
    }

    [Fact]
    public async Task Overview_PopularServices_RankedByBusyBookings()
    {
        using var db = NewDb();
        SeedRefs(db);
        db.Bookings.AddRange(
            Bk(M1, S1, BookingStatus.Completed, Day, 30),
            Bk(M1, S1, BookingStatus.Confirmed, Day.AddHours(1), 30),
            Bk(M2, S2, BookingStatus.Completed, Day, 20),
            Bk(M2, S1, BookingStatus.Cancelled, Day.AddHours(2), 30)); // не занимает
        await db.SaveChangesAsync();

        var overview = await new AnalyticsService(db).GetOverviewAsync(new AnalyticsQuery(null, null));

        Assert.Equal(2, overview.PopularServices.Count);
        Assert.Equal(S1, overview.PopularServices[0].ServiceId);
        Assert.Equal(2, overview.PopularServices[0].Bookings);
        Assert.Equal(1, overview.PopularServices.Single(s => s.ServiceId == S2).Bookings);
    }

    [Fact]
    public async Task Overview_RespectsDateRange()
    {
        using var db = NewDb();
        SeedRefs(db);
        db.Bookings.AddRange(
            Bk(M1, S1, BookingStatus.Completed, Day, 30),               // в периоде
            Bk(M1, S1, BookingStatus.Completed, Day.AddDays(10), 30));  // вне периода
        await db.SaveChangesAsync();

        var from = Day.AddDays(-1);
        var to = Day.AddDays(1);
        var overview = await new AnalyticsService(db).GetOverviewAsync(new AnalyticsQuery(from, to));

        Assert.Equal(1, overview.TotalBookings);
        Assert.Equal(from, overview.From);
        Assert.Equal(to, overview.To);
    }

    [Fact]
    public async Task Overview_Empty_ReturnsZeroes()
    {
        using var db = NewDb();
        SeedRefs(db);
        await db.SaveChangesAsync();

        var overview = await new AnalyticsService(db).GetOverviewAsync(new AnalyticsQuery(null, null));

        Assert.Equal(0, overview.TotalBookings);
        Assert.Empty(overview.ByStatus);
        Assert.Empty(overview.MasterLoad);
        Assert.Empty(overview.PopularServices);
    }
}
