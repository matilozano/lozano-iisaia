using Carroza.Api.Application.Services;
namespace Carroza.Api.Infrastructure.Lifecycle;

public sealed class SequenceLifetime(IComponentService components) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => components.StopAllAsync(CancellationToken.None);
}
