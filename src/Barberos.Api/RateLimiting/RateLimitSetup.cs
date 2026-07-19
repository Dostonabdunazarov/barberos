using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Barberos.Api.RateLimiting;

/// <summary>
/// Ограничение частоты запросов (PLAN.md §3, §10). Две политики с разбиением по IP:
/// вход персонала (защита от перебора пароля) и публичное создание брони (защита от спама).
/// Настройки — секция "RateLimiting" конфигурации.
/// </summary>
public static class RateLimitSetup
{
    /// <summary>Политика для POST /api/auth/login.</summary>
    public const string LoginPolicy = "login";

    /// <summary>Политика для POST /api/bookings.</summary>
    public const string BookingPolicy = "booking";

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
        var opt = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>()
            ?? new RateLimitOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.AddPolicy(LoginPolicy, ctx => FixedWindowByIp(ctx, opt.Login));
            limiter.AddPolicy(BookingPolicy, ctx => FixedWindowByIp(ctx, opt.Booking));

            // Единый ответ 429 в формате ProblemDetails (как остальные ошибки API).
            limiter.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Слишком много запросов. Повторите попытку позже.",
                }, ct);
            };
        });

        return services;
    }

    /// <summary>Fixed-window лимит с разбиением по IP клиента (или "unknown", если IP не определён).</summary>
    private static RateLimitPartition<string> FixedWindowByIp(HttpContext ctx, RateLimitWindow window)
    {
        var key = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = window.PermitLimit,
            Window = TimeSpan.FromSeconds(window.WindowSeconds),
            QueueLimit = 0, // лишние запросы сразу отклоняем, а не ставим в очередь
        });
    }
}

/// <summary>Настройки rate limiting из секции "RateLimiting".</summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Вход персонала (перебор пароля).</summary>
    public RateLimitWindow Login { get; set; } = new(PermitLimit: 5, WindowSeconds: 60);

    /// <summary>Публичное создание брони (спам).</summary>
    public RateLimitWindow Booking { get; set; } = new(PermitLimit: 10, WindowSeconds: 60);
}

/// <summary>Окно лимита: сколько запросов (PermitLimit) за сколько секунд (WindowSeconds).</summary>
public sealed record RateLimitWindow(int PermitLimit, int WindowSeconds);
