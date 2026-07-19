using System.Security.Claims;
using Barberos.Domain.Enums;

namespace Barberos.Api.Auth;

/// <summary>Утилиты чтения данных вошедшего сотрудника из ClaimsPrincipal.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Id вошедшего пользователя (claim "sub"/NameIdentifier). Null, если не аутентифицирован.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(nameof(UserRole.Admin));
}
