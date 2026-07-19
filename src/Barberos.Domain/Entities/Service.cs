using Barberos.Domain.Common;

namespace Barberos.Domain.Entities;

/// <summary>Услуга барбершопа (стрижка, бритьё и т.п.).</summary>
public class Service : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    /// <summary>Буфер после услуги (уборка/подготовка), учитывается при расчёте слотов.</summary>
    public int BufferMinutes { get; set; }
    /// <summary>Цена в сумах (UZS).</summary>
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MasterService> MasterServices { get; set; } = new List<MasterService>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
