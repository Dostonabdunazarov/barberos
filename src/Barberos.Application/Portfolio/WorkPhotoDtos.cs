namespace Barberos.Application.Portfolio;

/// <summary>Фото работы мастера (портфолио). Url — публичный относительный путь.</summary>
public record WorkPhotoDto(Guid Id, string Url, int SortOrder);

/// <summary>Данные загружаемого файла (абстрагированы от IFormFile, чтобы Application не зависел от web).</summary>
public sealed class UploadFile
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long Length { get; init; }
}
