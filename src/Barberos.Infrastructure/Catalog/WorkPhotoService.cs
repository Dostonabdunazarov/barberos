using Barberos.Application.Abstractions;
using Barberos.Application.Common;
using Barberos.Application.Portfolio;
using Barberos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barberos.Infrastructure.Catalog;

/// <summary>Портфолио работ мастера: список, загрузка файлов, удаление.</summary>
public sealed class WorkPhotoService(IAppDbContext db, IFileStorage storage) : IWorkPhotoService
{
    private const long MaxFileBytes = 5 * 1024 * 1024; // 5 МБ
    private const int MaxPhotosPerMaster = 20;
    private const string Subfolder = "uploads/works";

    // Разрешённые типы: contentType → расширение.
    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    public async Task<IReadOnlyList<WorkPhotoDto>> ListAsync(Guid masterId, CancellationToken ct = default)
    {
        await EnsureMasterExistsAsync(masterId, ct);

        return await db.WorkPhotos.AsNoTracking()
            .Where(p => p.MasterId == masterId)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.CreatedAt)
            .Select(p => new WorkPhotoDto(p.Id, p.Url, p.SortOrder))
            .ToListAsync(ct);
    }

    public async Task<WorkPhotoDto> AddAsync(Guid masterId, UploadFile file, CancellationToken ct = default)
    {
        await EnsureMasterExistsAsync(masterId, ct);

        // Валидация типа.
        if (!AllowedTypes.TryGetValue(file.ContentType, out var extension))
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = ["Поддерживаются только JPG, PNG или WebP."],
            });

        // Валидация размера.
        if (file.Length <= 0 || file.Length > MaxFileBytes)
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = [$"Размер файла должен быть от 1 байта до {MaxFileBytes / (1024 * 1024)} МБ."],
            });

        // Лимит числа фото на мастера.
        var count = await db.WorkPhotos.CountAsync(p => p.MasterId == masterId, ct);
        if (count >= MaxPhotosPerMaster)
            throw new ConflictException($"Достигнут лимит фото ({MaxPhotosPerMaster}). Удалите лишние.");

        var url = await storage.SaveAsync(file.Content, Subfolder, extension, ct);

        // Новое фото — в конец галереи.
        var maxOrder = count == 0
            ? -1
            : await db.WorkPhotos.Where(p => p.MasterId == masterId).MaxAsync(p => p.SortOrder, ct);

        var photo = new WorkPhoto
        {
            MasterId = masterId,
            Url = url,
            SortOrder = maxOrder + 1,
        };
        db.WorkPhotos.Add(photo);
        await db.SaveChangesAsync(ct);

        return new WorkPhotoDto(photo.Id, photo.Url, photo.SortOrder);
    }

    public async Task DeleteAsync(Guid masterId, Guid photoId, CancellationToken ct = default)
    {
        var photo = await db.WorkPhotos
            .FirstOrDefaultAsync(p => p.Id == photoId && p.MasterId == masterId, ct)
            ?? throw new NotFoundException("Фото не найдено.");

        db.WorkPhotos.Remove(photo);
        await db.SaveChangesAsync(ct);

        // Файл удаляем после успешного коммита записи.
        storage.Delete(photo.Url);
    }

    private async Task EnsureMasterExistsAsync(Guid masterId, CancellationToken ct)
    {
        if (!await db.Masters.AnyAsync(m => m.Id == masterId, ct))
            throw new NotFoundException("Мастер не найден.");
    }
}
