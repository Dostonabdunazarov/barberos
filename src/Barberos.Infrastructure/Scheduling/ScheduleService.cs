using Barberos.Application.Abstractions;
using Barberos.Application.Common;
using Barberos.Application.Scheduling;
using Barberos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barberos.Infrastructure.Scheduling;

/// <summary>Управление недельным расписанием и периодами недоступности мастеров.</summary>
public sealed class ScheduleService(IAppDbContext db) : IScheduleService
{
    public async Task<IReadOnlyList<ScheduleEntryDto>> GetScheduleAsync(Guid masterId, CancellationToken ct = default)
    {
        await EnsureMasterExistsAsync(masterId, ct);

        return await db.Schedules.AsNoTracking()
            .Where(s => s.MasterId == masterId)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new ScheduleEntryDto(s.DayOfWeek, s.StartTime, s.EndTime))
            .ToListAsync(ct);
    }

    public async Task SetScheduleAsync(Guid masterId, SetScheduleRequest request, CancellationToken ct = default)
    {
        await EnsureMasterExistsAsync(masterId, ct);

        // PUT-семантика: заменяем весь набор интервалов мастера.
        var existing = await db.Schedules.Where(s => s.MasterId == masterId).ToListAsync(ct);
        db.Schedules.RemoveRange(existing);

        foreach (var entry in request.Entries)
        {
            db.Schedules.Add(new Schedule
            {
                MasterId = masterId,
                DayOfWeek = entry.DayOfWeek,
                StartTime = entry.StartTime,
                EndTime = entry.EndTime,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TimeOffDto>> ListTimeOffAsync(Guid masterId, CancellationToken ct = default)
    {
        await EnsureMasterExistsAsync(masterId, ct);

        var now = DateTime.UtcNow;
        return await db.TimeOffs.AsNoTracking()
            .Where(t => t.MasterId == masterId && t.EndAt > now)
            .OrderBy(t => t.StartAt)
            .Select(t => new TimeOffDto(t.Id, t.StartAt, t.EndAt, t.Reason))
            .ToListAsync(ct);
    }

    public async Task<TimeOffDto> AddTimeOffAsync(Guid masterId, CreateTimeOffRequest request, CancellationToken ct = default)
    {
        await EnsureMasterExistsAsync(masterId, ct);

        var timeOff = new TimeOff
        {
            MasterId = masterId,
            StartAt = DateTime.SpecifyKind(request.StartAt, DateTimeKind.Utc),
            EndAt = DateTime.SpecifyKind(request.EndAt, DateTimeKind.Utc),
            Reason = request.Reason?.Trim(),
        };
        db.TimeOffs.Add(timeOff);
        await db.SaveChangesAsync(ct);

        return new TimeOffDto(timeOff.Id, timeOff.StartAt, timeOff.EndAt, timeOff.Reason);
    }

    public async Task RemoveTimeOffAsync(Guid masterId, Guid timeOffId, CancellationToken ct = default)
    {
        var timeOff = await db.TimeOffs
            .FirstOrDefaultAsync(t => t.Id == timeOffId && t.MasterId == masterId, ct)
            ?? throw new NotFoundException("Период недоступности не найден.");

        db.TimeOffs.Remove(timeOff);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureMasterExistsAsync(Guid masterId, CancellationToken ct)
    {
        if (!await db.Masters.AnyAsync(m => m.Id == masterId, ct))
            throw new NotFoundException("Мастер не найден.");
    }
}
