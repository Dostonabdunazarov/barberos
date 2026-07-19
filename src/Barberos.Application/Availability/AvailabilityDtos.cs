namespace Barberos.Application.Availability;

/// <summary>
/// Один доступный слот. StartAt/EndAt — в UTC (ISO 8601 с Z).
/// EndAt включает буфер услуги (интервал занятости мастера).
/// </summary>
public record SlotDto(DateTime StartAt, DateTime EndAt);

/// <summary>Ответ по доступности: запрошенная дата (в зоне барбершопа) и список слотов.</summary>
public record AvailabilityDto(DateOnly Date, IReadOnlyList<SlotDto> Slots);
