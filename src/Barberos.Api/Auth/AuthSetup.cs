using System.Text;
using Barberos.Domain.Enums;
using Barberos.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Barberos.Api.Auth;

public static class AuthSetup
{
    /// <summary>Роли для policy-based авторизации.</summary>
    public const string AdminPolicy = "Admin";
    public const string StaffPolicy = "Staff"; // Master или Admin

    public static IServiceCollection AddApiAuth(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Параметры валидации берём из уже забинденных JwtOptions (Infrastructure).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AdminPolicy, p => p.RequireRole(nameof(UserRole.Admin)))
            .AddPolicy(StaffPolicy, p => p.RequireRole(nameof(UserRole.Master), nameof(UserRole.Admin)));

        return services;
    }
}
