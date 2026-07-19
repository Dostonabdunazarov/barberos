using Barberos.Application.Abstractions;
using Barberos.Application.Auth;
using Barberos.Application.Availability;
using Barberos.Application.Bookings;
using Barberos.Application.Common;
using Barberos.Application.Masters;
using Barberos.Application.Scheduling;
using Barberos.Application.Services;
using Barberos.Infrastructure.Auth;
using Barberos.Infrastructure.Bookings;
using Barberos.Infrastructure.Catalog;
using Barberos.Infrastructure.Persistence;
using Barberos.Infrastructure.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barberos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' не задана.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Auth
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Key) && o.Key.Length >= 32,
                "Jwt:Key должен быть задан и содержать не менее 32 символов.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<AdminBootstrapper>();

        // Настройки барбершопа (таймзона, шаг слотов, lead time)
        services.AddOptions<BarbershopOptions>()
            .Bind(configuration.GetSection(BarbershopOptions.SectionName))
            .Validate(o => o.SlotStepMinutes is > 0 and <= 240, "Barbershop:SlotStepMinutes должен быть в (0, 240].")
            .Validate(o => o.LeadTimeMinutes >= 0, "Barbershop:LeadTimeMinutes не может быть отрицательным.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.TimeZone), "Barbershop:TimeZone обязателен.")
            .ValidateOnStart();

        // Каталог и расписание (Этап 2)
        services.AddScoped<IServiceCatalog, ServiceCatalog>();
        services.AddScoped<IMasterCatalog, MasterCatalog>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();

        // Бронирование (Этап 3)
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}
