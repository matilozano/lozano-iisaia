using Carroza.Api.Domain.Components;
using Carroza.Api.Domain.Commands;

namespace Carroza.Api.Application.Services;

public sealed class ComponentService(ComponentCatalog catalog, IComponentGateway gateway, OperationCoordinator? coordinator = null) : IComponentService
{
    private readonly OperationCoordinator operations = coordinator ?? new();
    private readonly ComponentCommandExecutor executor = new(catalog, gateway);

    public async Task<IReadOnlyList<ComponentSnapshot>> GetComponentsAsync(CancellationToken cancellationToken)
    {
        var result = new List<ComponentSnapshot>();
        foreach (var component in catalog.Components)
            result.Add(new(component, await gateway.GetStateAsync(component.Id, cancellationToken)));
        return result;
    }

    public async Task<ComponentState?> GetStateAsync(string id, CancellationToken cancellationToken) =>
        catalog.Find(id) is null ? null : await gateway.GetStateAsync(id, cancellationToken);

    public Task<ComponentResult?> ExecuteAsync(string id, ComponentCommand command, CancellationToken cancellationToken) =>
        operations.ManualAsync(() => executor.ExecuteAsync(id, command, cancellationToken), cancellationToken);
    public async Task<IReadOnlyList<ComponentSnapshot>> StopAllAndGetStatesAsync(CancellationToken cancellationToken)
    {
        var states = await operations.StopAsync(() => gateway.StopAllAndGetStatesAsync(CancellationToken.None));
        return states.Select(state => new ComponentSnapshot(catalog.Find(state.Id)!, state)).ToArray();
    }

    // Once requested, a client disconnect must not prevent STOP ALL.
    public async Task StopAllAsync(CancellationToken cancellationToken) => await StopAllAndGetStatesAsync(cancellationToken);
}
