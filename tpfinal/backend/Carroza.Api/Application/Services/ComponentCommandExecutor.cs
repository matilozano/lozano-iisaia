using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Components;
namespace Carroza.Api.Application.Services;

// One validation/execution path shared by manual requests and automatic steps.
public sealed class ComponentCommandExecutor(ComponentCatalog catalog, IComponentGateway gateway)
{
    public async Task<ComponentResult?> ExecuteAsync(string id, ComponentCommand command, CancellationToken ct)
    {
        var component = catalog.Find(id);
        if (component is null) return null;
        CommandValidator.Validate(component, command);
        return await gateway.ExecuteAsync(id, command, ct);
    }
}
