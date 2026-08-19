using Barberos.Application.Abstractions;
using Barberos.Application.Common;
using Barberos.Application.Portfolio;
using Barberos.Domain.Entities;
using Barberos.Infrastructure.Catalog;
using Barberos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Barberos.UnitTests;

/// <summary>
/// Тесты портфолио работ мастера: загрузка, список по порядку, удаление,
/// валидация типа/размера, лимит числа фото. Файловое хранилище — в памяти (без диска).
/// </summary>
public class WorkPhotoServiceTests
{
    private static readonly Guid MasterId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static void SeedMaster(AppDbContext db)
    {
        db.Masters.Add(new Master { Id = MasterId, Name = "Тест", IsActive = true });
        db.SaveChanges();
    }

    /// <summary>Фейковое хранилище: считает вызовы и хранит "сохранённые" URL в памяти.</summary>
    private sealed class FakeStorage : IFileStorage
    {
        public readonly List<string> Saved = [];
        public readonly List<string> Deleted = [];
        private int _n;

        public Task<string> SaveAsync(Stream content, string subfolder, string extension, CancellationToken ct = default)
        {
            var url = $"/{subfolder}/{++_n}{extension}";
            Saved.Add(url);
            return Task.FromResult(url);
        }

        public void Delete(string url) => Deleted.Add(url);
    }

    private static UploadFile Jpg(long length = 1000) => new()
    {
        Content = new MemoryStream([1, 2, 3]),
        FileName = "work.jpg",
        ContentType = "image/jpeg",
        Length = length,
    };

    [Fact]
    public async Task Add_ValidJpg_SavesFileAndPersists()
    {
        using var db = NewDb();
        SeedMaster(db);
        var storage = new FakeStorage();

        var dto = await new WorkPhotoService(db, storage).AddAsync(MasterId, Jpg());

        Assert.Single(storage.Saved);
        Assert.Equal(0, dto.SortOrder);
        Assert.Equal(storage.Saved[0], dto.Url);
        Assert.Equal(1, await db.WorkPhotos.CountAsync());
    }

    [Fact]
    public async Task Add_AssignsIncreasingSortOrder()
    {
        using var db = NewDb();
        SeedMaster(db);
        var svc = new WorkPhotoService(db, new FakeStorage());

        var a = await svc.AddAsync(MasterId, Jpg());
        var b = await svc.AddAsync(MasterId, Jpg());

        Assert.Equal(0, a.SortOrder);
        Assert.Equal(1, b.SortOrder);
    }

    [Fact]
    public async Task Add_UnknownMaster_Throws()
    {
        using var db = NewDb();
        var svc = new WorkPhotoService(db, new FakeStorage());
        await Assert.ThrowsAsync<NotFoundException>(() => svc.AddAsync(MasterId, Jpg()));
    }

    [Fact]
    public async Task Add_DisallowedType_ThrowsValidation()
    {
        using var db = NewDb();
        SeedMaster(db);
        var svc = new WorkPhotoService(db, new FakeStorage());
        var gif = new UploadFile { Content = new MemoryStream(), FileName = "x.gif", ContentType = "image/gif", Length = 100 };
        await Assert.ThrowsAsync<ValidationAppException>(() => svc.AddAsync(MasterId, gif));
    }

    [Fact]
    public async Task Add_TooLarge_ThrowsValidation()
    {
        using var db = NewDb();
        SeedMaster(db);
        var svc = new WorkPhotoService(db, new FakeStorage());
        await Assert.ThrowsAsync<ValidationAppException>(() => svc.AddAsync(MasterId, Jpg(10 * 1024 * 1024)));
    }

    [Fact]
    public async Task Add_OverLimit_ThrowsConflict()
    {
        using var db = NewDb();
        SeedMaster(db);
        var svc = new WorkPhotoService(db, new FakeStorage());
        for (var i = 0; i < 20; i++)
            await svc.AddAsync(MasterId, Jpg());

        await Assert.ThrowsAsync<ConflictException>(() => svc.AddAsync(MasterId, Jpg()));
    }

    [Fact]
    public async Task List_ReturnsInSortOrder()
    {
        using var db = NewDb();
        SeedMaster(db);
        var svc = new WorkPhotoService(db, new FakeStorage());
        await svc.AddAsync(MasterId, Jpg());
        await svc.AddAsync(MasterId, Jpg());

        var list = await svc.ListAsync(MasterId);

        Assert.Equal(2, list.Count);
        Assert.Equal(0, list[0].SortOrder);
        Assert.Equal(1, list[1].SortOrder);
    }

    [Fact]
    public async Task Delete_RemovesRecordAndFile()
    {
        using var db = NewDb();
        SeedMaster(db);
        var storage = new FakeStorage();
        var svc = new WorkPhotoService(db, storage);
        var dto = await svc.AddAsync(MasterId, Jpg());

        await svc.DeleteAsync(MasterId, dto.Id);

        Assert.Equal(0, await db.WorkPhotos.CountAsync());
        Assert.Contains(dto.Url, storage.Deleted);
    }

    [Fact]
    public async Task Delete_UnknownPhoto_Throws()
    {
        using var db = NewDb();
        SeedMaster(db);
        var svc = new WorkPhotoService(db, new FakeStorage());
        await Assert.ThrowsAsync<NotFoundException>(() => svc.DeleteAsync(MasterId, Guid.NewGuid()));
    }
}
