using Carroza.Api.Application.Services;
namespace Carroza.Api.Infrastructure.Lifecycle;

public sealed class SequenceLifetime(IComponentService components, ITelemetryService telemetry) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => telemetry.InitializeAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => components.StopAllAsync(CancellationToken.None);
}
