using Barberos.Application.Abstractions;

namespace Barberos.Infrastructure.Storage;

/// <summary>Настройки локального файлового хранилища.</summary>
public sealed class LocalFileStorageOptions
{
    /// <summary>Корневая папка веб-статики (обычно <c>wwwroot</c>), куда пишутся подпапки uploads.</summary>
    public string RootPath { get; set; } = "wwwroot";
}

/// <summary>
/// Хранилище файлов на локальном диске. Пишет в <c>{RootPath}/{subfolder}/{guid}{ext}</c>
/// и отдаёт относительный URL <c>/{subfolder}/{guid}{ext}</c> (раздаётся через UseStaticFiles).
/// </summary>
public sealed class LocalFileStorage(LocalFileStorageOptions options) : IFileStorage
{
    private readonly string _root = options.RootPath;

    public async Task<string> SaveAsync(Stream content, string subfolder, string extension, CancellationToken ct = default)
    {
        var safeSub = subfolder.Trim('/', '\\');
        var dir = Path.Combine(_root, safeSub);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(dir, fileName);

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await content.CopyToAsync(fs, ct);

        // Публичный URL — с прямыми слэшами, ведущий слэш обязателен.
        return $"/{safeSub}/{fileName}".Replace('\\', '/');
    }

    public void Delete(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        // URL вида "/uploads/works/xxx.jpg" → путь относительно корня статики.
        var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_root, relative);

        // Защита от выхода за пределы корня (path traversal).
        var rootFull = Path.GetFullPath(_root);
        var targetFull = Path.GetFullPath(fullPath);
        if (!targetFull.StartsWith(rootFull, StringComparison.Ordinal))
            return;

        if (File.Exists(targetFull))
            File.Delete(targetFull);
    }
}
