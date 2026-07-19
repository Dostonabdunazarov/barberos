namespace Barberos.Application.Abstractions;

/// <summary>Абстракция SMS-провайдера. Конкретная реализация — в Infrastructure.</summary>
public interface ISmsSender
{
    Task SendAsync(string phone, string message, CancellationToken ct = default);
}
