using Carroza.Api.Domain.Components;
using Carroza.Api.Domain.Commands;

namespace Carroza.Api.Application.Services;

public record ComponentSnapshot(Component Component, ComponentState State);

public interface IComponentService
{
    Task<IReadOnlyList<ComponentSnapshot>> GetComponentsAsync(CancellationToken cancellationToken);
    Task<ComponentState?> GetStateAsync(string id, CancellationToken cancellationToken);
    Task<ComponentResult?> ExecuteAsync(string id, ComponentCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyList<ComponentSnapshot>> StopAllAndGetStatesAsync(CancellationToken cancellationToken);
    Task StopAllAsync(CancellationToken cancellationToken);
}
