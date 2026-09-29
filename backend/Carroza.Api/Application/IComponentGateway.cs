using Carroza.Api.Domain;

namespace Carroza.Api.Application;

public interface IComponentGateway
{
    Task<ComponentState> GetStateAsync(string id, CancellationToken cancellationToken);
    Task<ComponentResult> ExecuteAsync(string id, ComponentCommand command, CancellationToken cancellationToken);
    Task StopAllAsync(CancellationToken cancellationToken);
}
