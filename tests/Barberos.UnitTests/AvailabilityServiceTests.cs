using Barberos.Application.Common;
using Barberos.Domain.Entities;
using Barberos.Domain.Enums;
using Barberos.Infrastructure.Persistence;
using Barberos.Infrastructure.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Barberos.UnitTests;

/// <summary>
/// Тесты алгоритма расчёта слотов (PLAN.md §6). Зона барбершопа — Asia/Tashkent (UTC+5).
/// Даты берём далеко в будущем, чтобы lead time не обрезал слоты (кроме отдельного теста).
/// </summary>
public class AvailabilityServiceTests
{
    private const string Tz = "Asia/Tashkent";
    private static readonly Guid MasterId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ServiceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // 2030-01-07 — понедельник.
    private static readonly DateOnly FutureMonday = new(2030, 1, 7);

    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AvailabilityService NewService(AppDbContext db, int leadTimeMinutes = 120, int step = 15)
    {
        var opts = Options.Create(new BarbershopOptions
        {
            TimeZone = Tz,
            SlotStepMinutes = step,
            LeadTimeMinutes = leadTimeMinutes,
        });
        return new AvailabilityService(db, opts);
    }

    private static void SeedMasterService(AppDbContext db, int durationMin, int bufferMin)
    {
        db.Masters.Add(new Master { Id = MasterId, Name = "Тест", IsActive = true });
        db.Services.Add(new Service
        {
            Id = ServiceId,
            Name = "Стрижка",
            DurationMinutes = durationMin,
            BufferMinutes = bufferMin,
            IsActive = true,
        });
        db.MasterServices.Add(new MasterService { MasterId = MasterId, ServiceId = ServiceId });
    }

    private static void SeedSchedule(AppDbContext db, DayOfWeek day, TimeOnly start, TimeOnly end)
        => db.Schedules.Add(new Schedule { MasterId = MasterId, DayOfWeek = day, StartTime = start, EndTime = end });

    /// <summary>Локальное (Tashkent) время даты → UTC-инстант, для сборки ожидаемых значений.</summary>
    private static DateTime LocalToUtc(DateOnly date, int hour, int minute)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Tz);
        var local = DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(hour, minute)), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }

    [Fact]
    public async Task EmptySchedule_ReturnsNoSlots()
    {
        using var db = NewDb();
        SeedMasterService(db, durationMin: 30, bufferMin: 0);
        // Без расписания на этот день.
        await db.SaveChangesAsync();

        var result = await NewService(db).GetAvailabilityAsync(MasterId, ServiceId, FutureMonday);

        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task SimpleShift_ProducesGridAlignedSlots()
    {
        using var db = NewDb();
        SeedMasterService(db, durationMin: 30, bufferMin: 0); // слот = 30 мин
        SeedSchedule(db, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0)); // 09:00–11:00 локально
        await db.SaveChangesAsync();

        var result = await NewService(db, step: 15).GetAvailabilityAsync(MasterId, ServiceId, FutureMonday);

        // Шаг 15, слот 30, окно 120 мин → старты 09:00,09:15,...,10:30 = 7 слотов.
        Assert.Equal(7, result.Slots.Count);
        Assert.Equal(LocalToUtc(FutureMonday, 9, 0), result.Slots[0].StartAt);
        Assert.Equal(LocalToUtc(FutureMonday, 9, 30), result.Slots[0].EndAt);
        Assert.Equal(LocalToUtc(FutureMonday, 10, 30), result.Slots[^1].StartAt);
        Assert.Equal(LocalToUtc(FutureMonday, 11, 0), result.Slots[^1].EndAt);
    }

    [Fact]
    public async Task Buffer_ExtendsBusyInterval()
    {
        using var db = NewDb();
        SeedMasterService(db, durationMin: 30, bufferMin: 15); // слот = 45 мин
        SeedSchedule(db, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0)); // 60 мин
        await db.SaveChangesAsync();

        var result = await NewService(db, step: 15).GetAvailabilityAsync(MasterId, ServiceId, FutureMonday);

        // Окно 60, слот 45, шаг 15 → старты 09:00, 09:15 (09:15+45=10:00 ok), 09:30 → 10:15 > 10:00 нет.
        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(LocalToUtc(FutureMonday, 9, 0), result.Slots[0].StartAt);
        Assert.Equal(LocalToUtc(FutureMonday, 9, 15), result.Slots[1].StartAt);
    }

    [Fact]
    public async Task ConfirmedBooking_BlocksOverlappingSlots()
    {
        using var db = NewDb();
        SeedMasterService(db, durationMin: 30, bufferMin: 0);
        SeedSchedule(db, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0));
        // Бронь 09:30–10:00 локально (04:30–05:00 UTC).
        db.Bookings.Add(new Booking
        {
            MasterId = MasterId,
            ServiceId = ServiceId,
            GuestName = "Клиент",
            GuestPhone = "+998900000000",
            StartAt = LocalToUtc(FutureMonday, 9, 30),
            EndAt = LocalToUtc(FutureMonday, 10, 0),
            Status = BookingStatus.Confirmed,
        });
        await db.SaveChangesAsync();

        var result = await NewService(db, step: 15).GetAvailabilityAsync(MasterId, ServiceId, FutureMonday);
        var starts = result.Slots.Select(s => s.StartAt).ToHashSet();

        // 09:15–09:45 и 09:30–10:00 и 09:45–10:15 пересекают бронь → исключены.
        Assert.DoesNotContain(LocalToUtc(FutureMonday, 9, 15), starts);
        Assert.DoesNotContain(LocalToUtc(FutureMonday, 9, 30), starts);
        Assert.DoesNotContain(LocalToUtc(FutureMonday, 9, 45), starts);
        // 09:00–09:30 не пересекает → доступен. 10:00–10:30 сразу после брони → доступен.
        Assert.Contains(LocalToUtc(FutureMonday, 9, 0), starts);
        Assert.Contains(LocalToUtc(FutureMonday, 10, 0), starts);
    }

    [Fact]
    public async Task CancelledBooking_DoesNotBlock()
    {
        using var db = NewDb();
        SeedMasterService(db, durationMin: 30, bufferMin: 0);
        SeedSchedule(db, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0));
        db.Bookings.Add(new Booking
        {
            MasterId = MasterId,
            ServiceId = ServiceId,
            GuestName = "Клиент",
            GuestPhone = "+998900000000",
            StartAt = LocalToUtc(FutureMonday, 9, 0),
            EndAt = LocalToUtc(FutureMonday, 9, 30),
            Status = BookingStatus.Cancelled, // освобождает слот
        });
        await db.SaveChangesAsync();

        var result = await NewService(db, step: 15).GetAvailabilityAsync(MasterId, ServiceId, FutureMonday);
        var starts = result.Slots.Select(s => s.StartAt).ToHashSet();

        Assert.Contains(LocalToUtc(FutureMonday, 9, 0), starts);
    }

    [Fact]
    public async Task TimeOff_BlocksOverlappingSlots()
    {
        using var db = NewDb();
        SeedMasterService(db, durationMin: 30, bufferMin: 0);
        SeedSchedule(db, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0));
        db.TimeOffs.Add(new TimeOff
        {
            MasterId = MasterId,
            StartAt = LocalToUtc(FutureMonday, 9, 0),
            EndAt = LocalToUtc(FutureMonday, 10, 0),
            Reason = "Перерыв",
        });
        await db.SaveChangesAsync();

        var result = await NewService(db, step: 15).GetAvailabilityAsync(MasterId, ServiceId, FutureMonday);
        var starts = result.Slots.Select(s => s.StartAt).ToHashSet();

        // Все старты, пересекающие 09:00–10:00, исключены; первый доступный — 10:00.
        Assert.DoesNotContain(LocalToUtc(FutureMonday, 9, 0), starts);
        Assert.DoesNotContain(LocalToUtc(FutureMonday, 9, 45), starts);
        Assert.Contains(LocalToUtc(FutureMonday, 10, 0), starts);
    }

    [Fact]
    public async Task LeadTime_ExcludesSlotsTooSoon()
    {
        using var db = NewDb();
        SeedMasterService(db, durationMin: 30, bufferMin: 0);

        // Расписание на сегодня (в зоне барбершопа), покрывающее ближайшие часы.
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Tz);
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var today = DateOnly.FromDateTime(nowLocal);
        // Смена на весь день, чтобы гарантированно были и «слишком скорые», и валидные слоты.
        SeedSchedule(db, today.DayOfWeek, new TimeOnly(0, 0), new TimeOnly(23, 30));
        await db.SaveChangesAsync();

        var leadMinutes = 120;
        var result = await NewService(db, leadTimeMinutes: leadMinutes).GetAvailabilityAsync(MasterId, ServiceId, today);

        var earliestAllowed = DateTime.UtcNow.AddMinutes(leadMinutes);
        Assert.All(result.Slots, s => Assert.True(s.StartAt >= earliestAllowed));
    }

    [Fact]
    public async Task MasterDoesNotOfferService_Throws()
    {
        using var db = NewDb();
        db.Masters.Add(new Master { Id = MasterId, Name = "Тест", IsActive = true });
        db.Services.Add(new Service { Id = ServiceId, Name = "Стрижка", DurationMinutes = 30, IsActive = true });
        // Нет связи MasterService.
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            NewService(db).GetAvailabilityAsync(MasterId, ServiceId, FutureMonday));
    }
}
