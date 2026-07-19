using Barberos.Application.Abstractions;
using Barberos.Application.Common;
using Barberos.Application.Masters;
using Barberos.Domain.Entities;
using Barberos.Domain.Enums;
using Barberos.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Barberos.Infrastructure.Catalog;

/// <summary>Управление мастерами и их услугами поверх EF Core.</summary>
public sealed class MasterCatalog(IAppDbContext db, IPasswordHasher passwordHasher) : IMasterCatalog
{
    public async Task<IReadOnlyList<MasterDto>> ListAsync(bool onlyActive, CancellationToken ct = default)
    {
        var query = db.Masters.AsNoTracking().Include(m => m.MasterServices).AsQueryable();
        if (onlyActive)
            query = query.Where(m => m.IsActive);

        var masters = await query.OrderBy(m => m.Name).ToListAsync(ct);
        return masters.Select(ToDto).ToList();
    }

    public async Task<MasterDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var master = await db.Masters.AsNoTracking()
            .Include(m => m.MasterServices)
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Мастер не найден.");
        return ToDto(master);
    }

    public async Task<MasterDto> CreateAsync(CreateMasterRequest request, CancellationToken ct = default)
    {
        Guid? userId = null;
        if (!string.IsNullOrWhiteSpace(request.LoginEmail))
        {
            var email = request.LoginEmail.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email, ct))
                throw new ConflictException("Пользователь с таким email уже существует.");

            var user = new User
            {
                Email = email,
                PasswordHash = passwordHasher.Hash(request.LoginPassword!),
                Name = request.Name.Trim(),
                Role = UserRole.Master,
                IsActive = true,
            };
            db.Users.Add(user);
            userId = user.Id;
        }

        var master = new Master
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Bio = request.Bio?.Trim(),
            PhotoUrl = request.PhotoUrl?.Trim(),
            IsActive = true,
        };
        db.Masters.Add(master);

        await AssignServicesAsync(master, request.ServiceIds, ct);

        await db.SaveChangesAsync(ct);
        return ToDto(master);
    }

    public async Task<MasterDto> UpdateAsync(Guid id, UpdateMasterRequest request, CancellationToken ct = default)
    {
        var master = await db.Masters
            .Include(m => m.MasterServices)
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Мастер не найден.");

        master.Name = request.Name.Trim();
        master.Bio = request.Bio?.Trim();
        master.PhotoUrl = request.PhotoUrl?.Trim();
        master.IsActive = request.IsActive;

        if (request.ServiceIds is not null)
        {
            // Полная замена набора услуг мастера.
            db.MasterServices.RemoveRange(master.MasterServices);
            master.MasterServices.Clear();
            await AssignServicesAsync(master, request.ServiceIds, ct);
        }

        await db.SaveChangesAsync(ct);
        return ToDto(master);
    }

    /// <summary>Привязывает уникальный набор услуг к мастеру, проверяя их существование.</summary>
    private async Task AssignServicesAsync(Master master, IReadOnlyList<Guid>? serviceIds, CancellationToken ct)
    {
        if (serviceIds is null || serviceIds.Count == 0)
            return;

        var distinct = serviceIds.Distinct().ToList();
        var existing = await db.Services
            .Where(s => distinct.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);

        var missing = distinct.Except(existing).ToList();
        if (missing.Count > 0)
            throw new NotFoundException($"Услуги не найдены: {string.Join(", ", missing)}.");

        foreach (var serviceId in distinct)
            master.MasterServices.Add(new MasterService { ServiceId = serviceId });
    }

    private static MasterDto ToDto(Master m) => new(
        m.Id, m.Name, m.Bio, m.PhotoUrl, m.IsActive, m.UserId,
        m.MasterServices.Select(ms => ms.ServiceId).ToList());
}
