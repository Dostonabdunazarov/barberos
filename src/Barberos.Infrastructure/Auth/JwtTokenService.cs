using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Barberos.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Barberos.Infrastructure.Auth;

/// <summary>Выдаёт access-токены (JWT) и генерирует/хеширует refresh-токены.</summary>
public interface IJwtTokenService
{
    (string token, DateTime expiresAt) CreateAccessToken(User user);
    /// <summary>Возвращает сырой refresh-токен (для клиента) и его SHA-256 хеш (для БД).</summary>
    (string raw, string hash, DateTime expiresAt) CreateRefreshToken();
    /// <summary>Хеш сырого refresh-токена для поиска в БД.</summary>
    string HashRefreshToken(string raw);
}

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _o = options.Value;

    public (string token, DateTime expiresAt) CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_o.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
        };
        if (!string.IsNullOrWhiteSpace(user.Name))
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, user.Name));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _o.Issuer,
            audience: _o.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }

    public (string raw, string hash, DateTime expiresAt) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes);
        return (raw, HashRefreshToken(raw), DateTime.UtcNow.AddDays(_o.RefreshTokenDays));
    }

    public string HashRefreshToken(string raw)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }
}
