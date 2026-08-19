using Barberos.Application.Abstractions;
using Barberos.Domain.Common;
using Barberos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Barberos.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Master> Masters => Set<Master>();
    public DbSet<MasterService> MasterServices => Set<MasterService>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<TimeOff> TimeOffs => Set<TimeOff>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<WorkPhoto> WorkPhotos => Set<WorkPhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Id у всех сущностей задаётся в коде (BaseEntity: Guid.NewGuid()), БД его не генерирует.
        // Без этого EF по соглашению трактует ValueGeneratedOnAdd + непустой Guid как уже
        // существующую строку и шлёт UPDATE вместо INSERT для добавленных в коллекцию связей
        // (DbUpdateConcurrencyException при добавлении услуг мастеру в UpdateAsync).
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entity.ClrType))
                entity.FindProperty(nameof(BaseEntity.Id))!.ValueGenerated = ValueGenerated.Never;
        }
    }
}
