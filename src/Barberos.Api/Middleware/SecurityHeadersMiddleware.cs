namespace Barberos.Api.Middleware;

/// <summary>
/// Проставляет базовые заголовки безопасности на все ответы API.
/// API отдаёт только JSON и не встраивается во фреймы — политика строгая.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Запрет MIME-sniffing.
        headers["X-Content-Type-Options"] = "nosniff";
        // API не предназначен для отображения во фреймах.
        headers["X-Frame-Options"] = "DENY";
        // Не утекать URL в Referer.
        headers["Referrer-Policy"] = "no-referrer";
        // JSON-API: запрещаем любую подгрузку ресурсов и встраивание.
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        // Ограничиваем доступ к браузерным API (на случай ошибочного HTML-ответа).
        headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";

        await next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
