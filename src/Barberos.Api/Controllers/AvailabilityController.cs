using Barberos.Application.Availability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barberos.Api.Controllers;

/// <summary>Свободные слоты для (мастер, услуга, дата). Публичный доступ.</summary>
[ApiController]
[Route("api/availability")]
public class AvailabilityController(IAvailabilityService availability) : ControllerBase
{
    /// <summary>GET /api/availability?masterId=&amp;serviceId=&amp;date=YYYY-MM-DD (дата в зоне барбершопа).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<AvailabilityDto>> Get(
        [FromQuery] Guid masterId,
        [FromQuery] Guid serviceId,
        [FromQuery] DateOnly date,
        CancellationToken ct)
    {
        if (masterId == Guid.Empty || serviceId == Guid.Empty)
            return BadRequest(new { message = "masterId и serviceId обязательны." });

        return Ok(await availability.GetAvailabilityAsync(masterId, serviceId, date, ct));
    }
}
