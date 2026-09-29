using Carroza.Api.Domain;

namespace Carroza.Api.Application.Services;

public sealed class ComponentService(ComponentCatalog catalog, IComponentGateway gateway) : IComponentService
{
    public async Task<IReadOnlyList<ComponentSnapshot>> GetComponentsAsync(CancellationToken cancellationToken)
    {
        var result = new List<ComponentSnapshot>();
        foreach (var component in catalog.Components)
            result.Add(new(component, await gateway.GetStateAsync(component.Id, cancellationToken)));
        return result;
    }

    public async Task<ComponentState?> GetStateAsync(string id, CancellationToken cancellationToken) =>
        catalog.Find(id) is null ? null : await gateway.GetStateAsync(id, cancellationToken);

    public async Task<ComponentResult?> ExecuteAsync(string id, ComponentCommand command, CancellationToken cancellationToken)
    {
        var component = catalog.Find(id);
        if (component is null) return null;
        CommandValidator.Validate(component, command);
        return await gateway.ExecuteAsync(id, command, cancellationToken);
    }

    // Once requested, a client disconnect must not prevent STOP ALL.
    public Task StopAllAsync(CancellationToken cancellationToken) => gateway.StopAllAsync(CancellationToken.None);
}
