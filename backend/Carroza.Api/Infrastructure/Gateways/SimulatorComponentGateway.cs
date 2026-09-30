using Carroza.Api.Application;
using Carroza.Api.Domain.Components;
using Carroza.Api.Domain.Commands;

namespace Carroza.Api.Infrastructure.Gateways;

public sealed class SimulatorComponentGateway(ComponentCatalog catalog, TimeProvider clock, SimulatorFaultPlan? faultPlan = null) : IComponentGateway
{
    private readonly object gate = new();
    private readonly SimulatorFaultPlan faults = faultPlan ?? new();
    private readonly Dictionary<string, SimulatedHydraulic> hydraulics = catalog.Components
        .Where(component => component.Type == "HYDRAULIC_ACTUATOR")
        .ToDictionary(component => component.Id, _ => new SimulatedHydraulic(clock));
    private readonly Dictionary<string, SimulatedLightBank> banks = catalog.Components
        .Where(component => component.Type == "light_bank")
        .ToDictionary(component => component.Id, _ => new SimulatedLightBank(clock));
    private readonly Dictionary<string, ComponentState> states = catalog.Components.ToDictionary(
        component => component.Id, component => new ComponentState(component.Id, RestingState(component.Type), true, Position: component.Type == "SERVO" ? 90 : null));

    public Task<ComponentState> GetStateAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate) return Task.FromResult(ReadState(id));
    }

    public Task<ComponentResult> ExecuteAsync(string id, ComponentCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            faults.BeforeCommand(id);
            if (hydraulics.TryGetValue(id, out var hydraulic))
                return Task.FromResult(new ComponentResult(id, true, hydraulic.Execute(id, command.Action!), clock.GetUtcNow()));
            if (banks.TryGetValue(id, out var bank))
                return Task.FromResult(new ComponentResult(id, true, bank.Execute(id, command), clock.GetUtcNow()));
            var current = states[id];
            var next = command.Action switch
            {
                "on" or "off" => current with { State = command.Action },
                "start" => current with { State = "running", Direction = command.Direction ?? "forward", Speed = command.Speed ?? 50 },
                "SET_POSITION" => current with { Position = command.Position },
                "stop" => current with { State = "stopped", Speed = 0 },
                _ => throw new ArgumentException("Comando no soportado por el simulador.")
            };
            states[id] = next;
            return Task.FromResult(new ComponentResult(id, true, next, clock.GetUtcNow()));
        }
    }

    public async Task StopAllAsync(CancellationToken cancellationToken) =>
        await StopAllAndGetStatesAsync(cancellationToken);

    public Task<IReadOnlyList<ComponentState>> StopAllAndGetStatesAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            // Fault injection never blocks the simulator safety operation.
            foreach (var pair in hydraulics) pair.Value.Execute(pair.Key, "STOP");
            foreach (var bank in banks.Values) bank.StopAll();
            foreach (var component in catalog.Components)
                states[component.Id] = states[component.Id] with { State = RestingState(component.Type), Speed = 0 };
            return Task.FromResult<IReadOnlyList<ComponentState>>(catalog.Components.Select(component => ReadState(component.Id)).ToArray());
        }
    }

    private ComponentState ReadState(string id)
    {
        var state = hydraulics.TryGetValue(id, out var hydraulic) ? hydraulic.Read(id)
            : banks.TryGetValue(id, out var bank) ? bank.Read(id) : states[id];
        return state with { Online = faults.IsOnline(id) };
    }
    private static string RestingState(string type) => type switch
    {
        "light" or "light_bank" => "off",
        "motor" or "HYDRAULIC_ACTUATOR" or "SERVO" => "stopped",
        _ => throw new NotSupportedException($"Tipo no simulado: {type}.")
    };
}
