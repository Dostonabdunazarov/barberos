namespace Barberos.Application.Portfolio;

/// <summary>Портфолио работ мастера: список, загрузка, удаление фото.</summary>
public interface IWorkPhotoService
{
    /// <summary>Все фото мастера в порядке отображения (публично).</summary>
    Task<IReadOnlyList<WorkPhotoDto>> ListAsync(Guid masterId, CancellationToken ct = default);

    /// <summary>Загрузить фото работы мастера. Валидирует тип/размер/лимит.</summary>
    Task<WorkPhotoDto> AddAsync(Guid masterId, UploadFile file, CancellationToken ct = default);

    /// <summary>Удалить фото (запись в БД + файл с диска).</summary>
    Task DeleteAsync(Guid masterId, Guid photoId, CancellationToken ct = default);
}
