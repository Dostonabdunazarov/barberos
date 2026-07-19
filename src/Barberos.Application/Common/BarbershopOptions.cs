namespace Barberos.Application.Common;

/// <summary>
/// Настройки барбершопа из секции "Barbershop" конфигурации.
/// Единая таймзона (IANA), шаг сетки слотов и минимальный буфер до записи.
/// </summary>
public sealed class BarbershopOptions
{
    public const string SectionName = "Barbershop";

    /// <summary>IANA-идентификатор таймзоны барбершопа (напр. "Asia/Tashkent").</summary>
    public string TimeZone { get; set; } = "Asia/Tashkent";

    /// <summary>Шаг сетки при генерации слотов, минуты.</summary>
    public int SlotStepMinutes { get; set; } = 15;

    /// <summary>Минимальный буфер от текущего момента до начала слота, минуты (lead time).</summary>
    public int LeadTimeMinutes { get; set; } = 120;
}
