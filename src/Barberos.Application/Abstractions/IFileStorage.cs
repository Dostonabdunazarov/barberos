namespace Barberos.Application.Abstractions;

/// <summary>
/// Хранилище загружаемых файлов (фото работ и т.п.). Абстрагирует физическое место
/// хранения (локальный диск, S3…), чтобы сервисы и тесты не зависели от файловой системы.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Сохраняет содержимое в подпапке и возвращает публичный относительный URL
    /// (напр. <c>/uploads/works/{guid}.jpg</c>).
    /// </summary>
    /// <param name="content">Поток данных файла.</param>
    /// <param name="subfolder">Логическая подпапка (напр. "works").</param>
    /// <param name="extension">Расширение с точкой (напр. ".jpg").</param>
    Task<string> SaveAsync(Stream content, string subfolder, string extension, CancellationToken ct = default);

    /// <summary>Удаляет файл по ранее выданному относительному URL. Молча игнорирует отсутствующий.</summary>
    void Delete(string url);
}
