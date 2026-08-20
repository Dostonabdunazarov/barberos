using Barberos.Application.Abstractions;
using Barberos.Application.Common;
using Barberos.Application.Masters;
using Barberos.Domain.Entities;
using Barberos.Infrastructure.Auth;
using Barberos.Infrastructure.Catalog;
using Barberos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Barberos.UnitTests;

/// <summary>
/// Публичный контакт мастера: нормализация при сохранении и правила валидации.
/// Поле необязательное — пустое значение означает «контакт не показываем».
/// </summary>
public class MasterContactTests
{
    private static readonly Guid MasterId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "hash:" + password;
        public (bool ok, bool needsRehash) Verify(string hash, string password) => (true, false);
    }

    private sealed class FakeStorage : IFileStorage
    {
        public Task<string> SaveAsync(Stream content, string subfolder, string extension, CancellationToken ct = default)
            => Task.FromResult($"/{subfolder}/x{extension}");
        public void Delete(string url) { }
    }

    private static MasterCatalog NewCatalog(AppDbContext db) => new(db, new FakeHasher(), new FakeStorage());

    private static void SeedMaster(AppDbContext db, string? phone = null)
    {
        db.Masters.Add(new Master { Id = MasterId, Name = "Тест", PublicPhone = phone, IsActive = true });
        db.SaveChanges();
    }

    [Fact]
    public async Task UpdateContact_Sets_Phone()
    {
        using var db = NewDb();
        SeedMaster(db);

        var dto = await NewCatalog(db).UpdateContactAsync(MasterId, new UpdateMasterContactRequest("+998 90 123-45-67"));

        Assert.Equal("+998 90 123-45-67", dto.PublicPhone);
    }

    [Fact]
    public async Task UpdateContact_Trims_Whitespace()
    {
        using var db = NewDb();
        SeedMaster(db);

        var dto = await NewCatalog(db).UpdateContactAsync(MasterId, new UpdateMasterContactRequest("  +998901234567  "));

        Assert.Equal("+998901234567", dto.PublicPhone);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateContact_Empty_Clears_Phone(string? input)
    {
        using var db = NewDb();
        SeedMaster(db, "+998901234567");

        var dto = await NewCatalog(db).UpdateContactAsync(MasterId, new UpdateMasterContactRequest(input));

        Assert.Null(dto.PublicPhone);
    }

    [Fact]
    public async Task UpdateContact_Unknown_Master_Throws_NotFound()
    {
        using var db = NewDb();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            NewCatalog(db).UpdateContactAsync(Guid.NewGuid(), new UpdateMasterContactRequest("+998901234567")));
    }

    [Fact]
    public async Task UpdateContact_Does_Not_Touch_Other_Fields()
    {
        using var db = NewDb();
        db.Masters.Add(new Master
        {
            Id = MasterId,
            Name = "Али",
            Bio = "Био",
            PhotoUrl = "/uploads/masters/1.jpg",
            IsActive = true,
        });
        db.SaveChanges();

        var dto = await NewCatalog(db).UpdateContactAsync(MasterId, new UpdateMasterContactRequest("+998901234567"));

        Assert.Equal("Али", dto.Name);
        Assert.Equal("Био", dto.Bio);
        Assert.Equal("/uploads/masters/1.jpg", dto.PhotoUrl);
        Assert.True(dto.IsActive);
    }

    // ── Валидация ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("+998901234567")]
    [InlineData("+998 90 123-45-67")]
    [InlineData("(90) 123-45-67")]
    public void Validator_Accepts_Empty_And_Valid_Phones(string? phone)
    {
        var result = new UpdateMasterContactRequestValidator()
            .Validate(new UpdateMasterContactRequest(phone));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("12345")]                    // короче 7 символов
    [InlineData("не телефон")]               // буквы
    [InlineData("+998901234567890123456")]   // длиннее 20
    public void Validator_Rejects_Malformed_Phone(string phone)
    {
        var result = new UpdateMasterContactRequestValidator()
            .Validate(new UpdateMasterContactRequest(phone));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMasterContactRequest.PublicPhone));
    }
}
