using System.Security.Claims;
using Barberos.Api.RateLimiting;
using Barberos.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Barberos.Api.Controllers;

/// <summary>Аутентификация персонала (мастер/админ). Клиенты не аутентифицируются.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    private const string RefreshCookie = "refreshToken";

    /// <summary>Ответ входа: access-токен в теле, refresh — в httpOnly cookie.</summary>
    public record LoginResponse(string AccessToken, DateTime AccessTokenExpiresAt, AuthUserDto User);

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitSetup.LoginPolicy)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);
        return Ok(ToResponse(result));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh(CancellationToken ct)
    {
        var token = Request.Cookies[RefreshCookie];
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new { message = "Refresh-токен отсутствует." });

        var result = await auth.RefreshAsync(token, ct);
        return Ok(ToResponse(result));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = Request.Cookies[RefreshCookie];
        if (!string.IsNullOrEmpty(token))
            await auth.LogoutAsync(token, ct);

        Response.Cookies.Delete(RefreshCookie);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        await auth.ChangePasswordAsync(userId, request, ct);

        // Все сессии отозваны — чистим cookie текущей.
        Response.Cookies.Delete(RefreshCookie);
        return NoContent();
    }

    private LoginResponse ToResponse(AuthResult result)
    {
        Response.Cookies.Append(RefreshCookie, result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None, // SPA на другом origin
            Expires = result.RefreshTokenExpiresAt,
            Path = "/api/auth",
        });
        return new LoginResponse(result.AccessToken, result.AccessTokenExpiresAt, result.User);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new AuthException("Токен без идентификатора пользователя.");
        return Guid.Parse(sub);
    }
}
