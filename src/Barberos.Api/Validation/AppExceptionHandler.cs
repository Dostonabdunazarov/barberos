using Barberos.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Barberos.Api.Validation;

/// <summary>
/// Маппит доменные исключения Application-слоя в HTTP-ответы:
/// NotFound → 404, Conflict → 409, Forbidden → 403, Validation → 400 (ProblemDetails).
/// </summary>
public sealed class AppExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception exception, CancellationToken ct)
    {
        switch (exception)
        {
            case ValidationAppException vex:
                await WriteValidationAsync(ctx, vex, ct);
                return true;

            case NotFoundException:
                await WriteAsync(ctx, StatusCodes.Status404NotFound, exception.Message, ct);
                return true;

            case ConflictException:
                await WriteAsync(ctx, StatusCodes.Status409Conflict, exception.Message, ct);
                return true;

            case ForbiddenException:
                await WriteAsync(ctx, StatusCodes.Status403Forbidden, exception.Message, ct);
                return true;

            default:
                return false;
        }
    }

    private static async Task WriteAsync(HttpContext ctx, int status, string message, CancellationToken ct)
    {
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = message,
        }, ct);
    }

    private static async Task WriteValidationAsync(HttpContext ctx, ValidationAppException vex, CancellationToken ct)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        var problem = new ValidationProblemDetails(vex.Errors.ToDictionary(kv => kv.Key, kv => kv.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Ошибка валидации входных данных.",
        };
        await ctx.Response.WriteAsJsonAsync(problem, ct);
    }
}
