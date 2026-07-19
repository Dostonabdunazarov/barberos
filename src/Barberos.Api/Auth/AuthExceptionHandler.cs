using Barberos.Application.Auth;
using Microsoft.AspNetCore.Diagnostics;

namespace Barberos.Api.Auth;

/// <summary>Маппит <see cref="AuthException"/> в 401 без раскрытия деталей стека.</summary>
public sealed class AuthExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx, Exception exception, CancellationToken ct)
    {
        if (exception is not AuthException authEx)
            return false;

        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await ctx.Response.WriteAsJsonAsync(new { message = authEx.Message }, ct);
        return true;
    }
}
