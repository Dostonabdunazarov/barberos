using Barberos.Api.Auth;
using Barberos.Application.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barberos.Api.Controllers;

/// <summary>Базовая админ-аналитика (только admin).</summary>
[ApiController]
[Route("api/analytics")]
[Authorize(Policy = AuthSetup.AdminPolicy)]
public class AnalyticsController(IAnalyticsService analytics) : ControllerBase
{
    /// <summary>
    /// Сводка за период [from, to) по StartAt (UTC): брони по статусам,
    /// загрузка мастеров, популярные услуги. Границы опциональны.
    /// </summary>
    [HttpGet("overview")]
    public async Task<ActionResult<AnalyticsOverviewDto>> Overview(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await analytics.GetOverviewAsync(new AnalyticsQuery(from, to), ct));
}
