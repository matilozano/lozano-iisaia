using Carroza.Api.Application.Services;

namespace Carroza.Api.Infrastructure.Lifecycle;

public sealed class TelemetryMonitor(ITelemetryService telemetry, ILogger<TelemetryMonitor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await telemetry.ReadAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogWarning(error, "Supervisión de telemetría no disponible"); }
            try { await Task.Delay(250, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
