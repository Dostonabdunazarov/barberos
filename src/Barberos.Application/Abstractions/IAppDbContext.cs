using Barberos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barberos.Application.Abstractions;

/// <summary>Абстракция контекста БД для Application-слоя (реализация — в Infrastructure).</summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Service> Services { get; }
    DbSet<Master> Masters { get; }
    DbSet<MasterService> MasterServices { get; }
    DbSet<Schedule> Schedules { get; }
    DbSet<TimeOff> TimeOffs { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<Review> Reviews { get; }
    DbSet<WorkPhoto> WorkPhotos { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
