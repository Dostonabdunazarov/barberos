namespace Barberos.Infrastructure.Auth;

/// <summary>Настройки JWT из секции "Jwt" конфигурации.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    /// <summary>Секретный ключ подписи (HMAC-SHA256). Мин. 32 символа. Из env/secrets.</summary>
    public string Key { get; set; } = null!;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
