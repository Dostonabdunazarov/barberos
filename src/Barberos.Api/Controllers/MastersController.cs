using Barberos.Api.Auth;
using Barberos.Application.Common;
using Barberos.Application.Masters;
using Barberos.Application.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barberos.Api.Controllers;

/// <summary>
/// Мастера, их расписание и периоды недоступности.
/// Чтение профилей/расписания — публично; создание/изменение мастера — admin;
/// изменение расписания и time-off — admin или сам мастер (своё).
/// </summary>
[ApiController]
[Route("api/masters")]
public class MastersController(IMasterCatalog masters, IScheduleService schedule) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<MasterDto>>> List(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var onlyActive = !(includeInactive && User.IsAdmin());
        return Ok(await masters.ListAsync(onlyActive, ct));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<MasterDto>> Get(Guid id, CancellationToken ct)
        => Ok(await masters.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = AuthSetup.AdminPolicy)]
    public async Task<ActionResult<MasterDto>> Create(CreateMasterRequest request, CancellationToken ct)
    {
        var created = await masters.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthSetup.AdminPolicy)]
    public async Task<ActionResult<MasterDto>> Update(Guid id, UpdateMasterRequest request, CancellationToken ct)
        => Ok(await masters.UpdateAsync(id, request, ct));

    // ── Расписание ───────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/schedule")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ScheduleEntryDto>>> GetSchedule(Guid id, CancellationToken ct)
        => Ok(await schedule.GetScheduleAsync(id, ct));

    [HttpPut("{id:guid}/schedule")]
    [Authorize(Policy = AuthSetup.StaffPolicy)]
    public async Task<IActionResult> SetSchedule(Guid id, SetScheduleRequest request, CancellationToken ct)
    {
        await EnsureCanManageAsync(id, ct);
        await schedule.SetScheduleAsync(id, request, ct);
        return NoContent();
    }

    // ── Time-off ─────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/time-off")]
    [Authorize(Policy = AuthSetup.StaffPolicy)]
    public async Task<ActionResult<IReadOnlyList<TimeOffDto>>> ListTimeOff(Guid id, CancellationToken ct)
    {
        await EnsureCanManageAsync(id, ct);
        return Ok(await schedule.ListTimeOffAsync(id, ct));
    }

    [HttpPost("{id:guid}/time-off")]
    [Authorize(Policy = AuthSetup.StaffPolicy)]
    public async Task<ActionResult<TimeOffDto>> AddTimeOff(Guid id, CreateTimeOffRequest request, CancellationToken ct)
    {
        await EnsureCanManageAsync(id, ct);
        var created = await schedule.AddTimeOffAsync(id, request, ct);
        return CreatedAtAction(nameof(ListTimeOff), new { id }, created);
    }

    [HttpDelete("{id:guid}/time-off/{timeOffId:guid}")]
    [Authorize(Policy = AuthSetup.StaffPolicy)]
    public async Task<IActionResult> RemoveTimeOff(Guid id, Guid timeOffId, CancellationToken ct)
    {
        await EnsureCanManageAsync(id, ct);
        await schedule.RemoveTimeOffAsync(id, timeOffId, ct);
        return NoContent();
    }

    /// <summary>
    /// Разрешает операцию, если пользователь — admin или сам мастер (учётка привязана к этому мастеру).
    /// Бросает 403 иначе, 404 если мастер не найден.
    /// </summary>
    private async Task EnsureCanManageAsync(Guid masterId, CancellationToken ct)
    {
        if (User.IsAdmin())
        {
            // Всё равно убеждаемся, что мастер существует (иначе будет 404).
            await masters.GetAsync(masterId, ct);
            return;
        }

        var master = await masters.GetAsync(masterId, ct);
        var currentUserId = User.GetUserId();
        if (master.UserId is null || master.UserId != currentUserId)
            throw new ForbiddenException("Нельзя управлять расписанием другого мастера.");
    }
}
