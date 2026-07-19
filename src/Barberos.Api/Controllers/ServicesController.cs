using Barberos.Api.Auth;
using Barberos.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barberos.Api.Controllers;

/// <summary>Каталог услуг. Чтение — публично, изменение — только admin.</summary>
[ApiController]
[Route("api/services")]
public class ServicesController(IServiceCatalog catalog) : ControllerBase
{
    /// <summary>Список услуг. Публично — только активные; admin видит все (?includeInactive=true).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> List(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        // Неактивные видит только admin.
        var onlyActive = !(includeInactive && User.IsInRole(nameof(Barberos.Domain.Enums.UserRole.Admin)));
        return Ok(await catalog.ListAsync(onlyActive, ct));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceDto>> Get(Guid id, CancellationToken ct)
        => Ok(await catalog.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = AuthSetup.AdminPolicy)]
    public async Task<ActionResult<ServiceDto>> Create(CreateServiceRequest request, CancellationToken ct)
    {
        var created = await catalog.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthSetup.AdminPolicy)]
    public async Task<ActionResult<ServiceDto>> Update(Guid id, UpdateServiceRequest request, CancellationToken ct)
        => Ok(await catalog.UpdateAsync(id, request, ct));
}
