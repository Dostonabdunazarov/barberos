namespace Barberos.Application.Scheduling;

/// <summary>Один рабочий интервал мастера в конкретный день недели. Время локальное (зона барбершопа).</summary>
public record ScheduleEntryDto(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

/// <summary>
/// Полное недельное расписание мастера. PUT заменяет весь набор интервалов.
/// Допускается несколько интервалов в один день (напр. до и после обеда).
/// </summary>
public record SetScheduleRequest(IReadOnlyList<ScheduleEntryDto> Entries);

/// <summary>Период недоступности мастера (отпуск/перерыв). Время в UTC (ISO 8601 с Z).</summary>
public record TimeOffDto(Guid Id, DateTime StartAt, DateTime EndAt, string? Reason);

/// <summary>Создание периода недоступности. StartAt/EndAt — в UTC.</summary>
public record CreateTimeOffRequest(DateTime StartAt, DateTime EndAt, string? Reason);
