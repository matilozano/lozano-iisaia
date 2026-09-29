using Carroza.Api.Application;
using Carroza.Api.Domain;

namespace Carroza.Api.Infrastructure;

public sealed class SimulatorComponentGateway(ComponentCatalog catalog, TimeProvider clock) : IComponentGateway
{
    private readonly object gate = new();
    private readonly Dictionary<string, ComponentState> states = catalog.Components.ToDictionary(
        component => component.Id, component => new ComponentState(component.Id, RestingState(component.Type), true));

    public Task<ComponentState> GetStateAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate) return Task.FromResult(states[id]);
    }

    public Task<ComponentResult> ExecuteAsync(string id, ComponentCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            var current = states[id];
            var next = command.Action switch
            {
                "on" or "off" => current with { State = command.Action },
                "start" => current with { State = "running", Direction = command.Direction ?? "forward", Speed = command.Speed ?? 50 },
                "stop" => current with { State = "stopped", Speed = 0 },
                _ => throw new ArgumentException("Comando no soportado por el simulador.")
            };
            states[id] = next;
            return Task.FromResult(new ComponentResult(id, true, next, clock.GetUtcNow()));
        }
    }

    public Task StopAllAsync(CancellationToken cancellationToken)
    {
        lock (gate)
            foreach (var component in catalog.Components)
                states[component.Id] = states[component.Id] with { State = RestingState(component.Type), Speed = 0 };
        return Task.CompletedTask;
    }

    private static string RestingState(string type) => type switch
    {
        "light" => "off",
        "motor" => "stopped",
        _ => throw new NotSupportedException($"Tipo no simulado: {type}.")
    };
}
