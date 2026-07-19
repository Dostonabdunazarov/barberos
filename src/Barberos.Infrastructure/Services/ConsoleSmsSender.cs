using Barberos.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Barberos.Infrastructure.Services;

/// <summary>Заглушка SMS-отправки: пишет код в лог. Заменить на реального провайдера.</summary>
public class ConsoleSmsSender(ILogger<ConsoleSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string phone, string message, CancellationToken ct = default)
    {
        logger.LogInformation("[SMS] to {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}
