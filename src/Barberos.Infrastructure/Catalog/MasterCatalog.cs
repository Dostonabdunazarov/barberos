using Barberos.Application.Abstractions;
using Barberos.Application.Common;
using Barberos.Application.Masters;
using Barberos.Application.Portfolio;
using Barberos.Domain.Entities;
using Barberos.Domain.Enums;
using Barberos.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Barberos.Infrastructure.Catalog;

/// <summary>Управление мастерами и их услугами поверх EF Core.</summary>
public sealed class MasterCatalog(IAppDbContext db, IPasswordHasher passwordHasher, IFileStorage storage) : IMasterCatalog
{
    private const long MaxPhotoBytes = 5 * 1024 * 1024; // 5 МБ

    // Разрешённые типы фото мастера: contentType → расширение.
    private static readonly Dictionary<string, string> AllowedPhotoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };
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
            PublicPhone = Normalize(request.PublicPhone),
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
        master.PublicPhone = Normalize(request.PublicPhone);
        master.IsActive = request.IsActive;

        if (request.ServiceIds is not null)
            await SyncServicesAsync(master, request.ServiceIds, ct);

        await SyncAccountAsync(master, request.LoginEmail, request.LoginPassword, ct);

        await db.SaveChangesAsync(ct);
        return ToDto(master);
    }

    public async Task<MasterDto> UpdateContactAsync(
        Guid id, UpdateMasterContactRequest request, CancellationToken ct = default)
    {
        var master = await db.Masters
            .Include(m => m.MasterServices)
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Мастер не найден.");

        master.PublicPhone = Normalize(request.PublicPhone);
        await db.SaveChangesAsync(ct);
        return ToDto(master);
    }

    /// <summary>Пустая строка и пробелы — это «контакта нет», храним как null.</summary>
    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// Приводит учётку мастера в соответствие с запросом (admin):
    /// - задан email → создаёт учётку (если её не было) или меняет email существующей;
    /// - задан пароль → сбрасывает пароль (учётка при этом должна существовать/создаваться email'ом);
    /// - оба пусты → ничего не делает.
    /// </summary>
    private async Task SyncAccountAsync(Master master, string? loginEmail, string? loginPassword, CancellationToken ct)
    {
        var email = loginEmail?.Trim().ToLowerInvariant();
        var hasEmail = !string.IsNullOrWhiteSpace(email);
        var hasPassword = !string.IsNullOrWhiteSpace(loginPassword);
        if (!hasEmail && !hasPassword)
            return;

        // Загружаем существующую учётку мастера (если есть).
        var user = master.UserId is { } uid
            ? await db.Users.FirstOrDefaultAsync(u => u.Id == uid, ct)
            : null;

        if (user is null)
        {
            // Создание учётки требует и email, и пароль.
            if (!hasEmail || !hasPassword)
                throw new ConflictException("Для создания учётной записи мастера нужны и email, и пароль.");

            if (await db.Users.AnyAsync(u => u.Email == email, ct))
                throw new ConflictException("Пользователь с таким email уже существует.");

            user = new User
            {
                Email = email!,
                PasswordHash = passwordHasher.Hash(loginPassword!),
                Name = master.Name,
                Role = UserRole.Master,
                IsActive = true,
            };
            db.Users.Add(user);
            master.UserId = user.Id;
            return;
        }

        // Смена email существующей учётки (с проверкой уникальности).
        if (hasEmail && email != user.Email)
        {
            if (await db.Users.AnyAsync(u => u.Email == email && u.Id != user.Id, ct))
                throw new ConflictException("Пользователь с таким email уже существует.");
            user.Email = email!;
        }

        // Сброс пароля.
        if (hasPassword)
            user.PasswordHash = passwordHasher.Hash(loginPassword!);
    }

    /// <summary>
    /// Приводит набор услуг мастера к желаемому: удаляет лишние связи и добавляет
    /// недостающие, не трогая уже существующие (дифф вместо remove-all + re-add,
    /// что ломало change-tracker и давало DbUpdateConcurrencyException).
    /// </summary>
    private async Task SyncServicesAsync(Master master, IReadOnlyList<Guid> serviceIds, CancellationToken ct)
    {
        var desired = serviceIds.Distinct().ToHashSet();

        // Проверяем существование всех запрошенных услуг (иначе 404).
        if (desired.Count > 0)
        {
            var existing = await db.Services
                .Where(s => desired.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync(ct);
            var missing = desired.Except(existing).ToList();
            if (missing.Count > 0)
                throw new NotFoundException($"Услуги не найдены: {string.Join(", ", missing)}.");
        }

        // Удаляем связи, которых больше нет в желаемом наборе.
        var toRemove = master.MasterServices.Where(ms => !desired.Contains(ms.ServiceId)).ToList();
        if (toRemove.Count > 0)
            db.MasterServices.RemoveRange(toRemove);

        // Добавляем недостающие.
        var current = master.MasterServices.Select(ms => ms.ServiceId).ToHashSet();
        foreach (var serviceId in desired.Except(current))
            master.MasterServices.Add(new MasterService { ServiceId = serviceId });
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

    public async Task<MasterDto> SetPhotoAsync(Guid id, UploadFile file, CancellationToken ct = default)
    {
        var master = await db.Masters
            .Include(m => m.MasterServices)
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Мастер не найден.");

        if (!AllowedPhotoTypes.TryGetValue(file.ContentType, out var extension))
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = ["Поддерживаются только JPG, PNG или WebP."],
            });

        if (file.Length <= 0 || file.Length > MaxPhotoBytes)
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = [$"Размер файла должен быть от 1 байта до {MaxPhotoBytes / (1024 * 1024)} МБ."],
            });

        var oldUrl = master.PhotoUrl;
        var url = await storage.SaveAsync(file.Content, "uploads/masters", extension, ct);
        master.PhotoUrl = url;
        await db.SaveChangesAsync(ct);

        // Старый файл удаляем только если это был загруженный нами файл (относительный /uploads/...).
        if (!string.IsNullOrEmpty(oldUrl) && oldUrl.StartsWith("/uploads/", StringComparison.Ordinal))
            storage.Delete(oldUrl);

        return ToDto(master);
    }

    private static MasterDto ToDto(Master m) => new(
        m.Id, m.Name, m.Bio, m.PhotoUrl, m.PublicPhone, m.IsActive, m.UserId,
        m.MasterServices.Select(ms => ms.ServiceId).ToList());
}
