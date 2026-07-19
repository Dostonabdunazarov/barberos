using Barberos.Application.Abstractions;
using Barberos.Application.Common;
using Barberos.Application.Services;
using Barberos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barberos.Infrastructure.Catalog;

/// <summary>Управление каталогом услуг поверх EF Core.</summary>
public sealed class ServiceCatalog(IAppDbContext db) : IServiceCatalog
{
    public async Task<IReadOnlyList<ServiceDto>> ListAsync(bool onlyActive, CancellationToken ct = default)
    {
        var query = db.Services.AsNoTracking();
        if (onlyActive)
            query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.Name)
            .Select(s => ToDto(s))
            .ToListAsync(ct);
    }

    public async Task<ServiceDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var service = await db.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("Услуга не найдена.");
        return ToDto(service);
    }

    public async Task<ServiceDto> CreateAsync(CreateServiceRequest request, CancellationToken ct = default)
    {
        var service = new Service
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            DurationMinutes = request.DurationMinutes,
            BufferMinutes = request.BufferMinutes,
            Price = request.Price,
            IsActive = true,
        };
        db.Services.Add(service);
        await db.SaveChangesAsync(ct);
        return ToDto(service);
    }

    public async Task<ServiceDto> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken ct = default)
    {
        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("Услуга не найдена.");

        service.Name = request.Name.Trim();
        service.Description = request.Description?.Trim();
        service.DurationMinutes = request.DurationMinutes;
        service.BufferMinutes = request.BufferMinutes;
        service.Price = request.Price;
        service.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);
        return ToDto(service);
    }

    private static ServiceDto ToDto(Service s) => new(
        s.Id, s.Name, s.Description, s.DurationMinutes, s.BufferMinutes, s.Price, s.IsActive);
}
