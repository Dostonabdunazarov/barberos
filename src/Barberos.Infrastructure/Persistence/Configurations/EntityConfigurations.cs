using Barberos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barberos.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).IsRequired().HasMaxLength(256);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.Name).HasMaxLength(120);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).IsRequired();
        b.HasIndex(x => x.TokenHash);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(120);
        b.Property(x => x.Price).HasColumnType("numeric(10,2)");
    }
}

public class MasterConfiguration : IEntityTypeConfiguration<Master>
{
    public void Configure(EntityTypeBuilder<Master> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(120);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class MasterServiceConfiguration : IEntityTypeConfiguration<MasterService>
{
    public void Configure(EntityTypeBuilder<MasterService> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.MasterId, x.ServiceId }).IsUnique();
        b.HasOne(x => x.Master).WithMany(m => m.MasterServices).HasForeignKey(x => x.MasterId);
        b.HasOne(x => x.Service).WithMany(s => s.MasterServices).HasForeignKey(x => x.ServiceId);
    }
}

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> b)
    {
        b.HasKey(x => x.Id);
        b.HasOne(x => x.Master).WithMany(m => m.Schedules).HasForeignKey(x => x.MasterId);
        b.HasIndex(x => new { x.MasterId, x.DayOfWeek });
    }
}

public class TimeOffConfiguration : IEntityTypeConfiguration<TimeOff>
{
    public void Configure(EntityTypeBuilder<TimeOff> b)
    {
        b.HasKey(x => x.Id);
        b.HasOne(x => x.Master).WithMany(m => m.TimeOffs).HasForeignKey(x => x.MasterId);
        b.HasIndex(x => new { x.MasterId, x.StartAt, x.EndAt });
    }
}

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.GuestName).IsRequired().HasMaxLength(120);
        b.Property(x => x.GuestPhone).IsRequired().HasMaxLength(20);
        b.HasIndex(x => x.ManageToken).IsUnique();
        b.HasIndex(x => x.GuestPhone); // поиск истории клиента по телефону

        b.HasOne(x => x.Master).WithMany(m => m.Bookings).HasForeignKey(x => x.MasterId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Service).WithMany(s => s.Bookings).HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Индекс для быстрого поиска пересечений по мастеру и времени.
        b.HasIndex(x => new { x.MasterId, x.StartAt });
        // ПРИМЕЧАНИЕ: защиту от двойного бронирования добавить как EXCLUDE-constraint
        // в отдельной миграции (raw SQL):
        //   ALTER TABLE "Bookings" ADD CONSTRAINT no_overlap
        //   EXCLUDE USING gist ("MasterId" WITH =, tstzrange("StartAt","EndAt") WITH &&)
        //   WHERE ("Status" IN (0,1,2));  -- pending/confirmed/completed
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.BookingId).IsUnique();
        b.HasOne(x => x.Booking).WithOne(bk => bk.Review).HasForeignKey<Review>(x => x.BookingId);
        b.HasOne(x => x.Master).WithMany(m => m.Reviews).HasForeignKey(x => x.MasterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Recipient).IsRequired().HasMaxLength(256);
        b.Property(x => x.Payload).IsRequired();
        b.HasIndex(x => new { x.Status, x.ScheduledFor });
    }
}
