using Carroza.Api.Domain;

namespace Carroza.Api.Application.Services;

public record ComponentSnapshot(Component Component, ComponentState State);

public interface IComponentService
{
    Task<IReadOnlyList<ComponentSnapshot>> GetComponentsAsync(CancellationToken cancellationToken);
    Task<ComponentState?> GetStateAsync(string id, CancellationToken cancellationToken);
    Task<ComponentResult?> ExecuteAsync(string id, ComponentCommand command, CancellationToken cancellationToken);
    Task StopAllAsync(CancellationToken cancellationToken);
}
