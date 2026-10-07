using Carroza.Api.Application;
using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Components;
using Carroza.Api.Infrastructure.Esp32;

namespace Carroza.Api.Infrastructure.Gateways;

public sealed class Esp32ComponentGateway(ComponentCatalog catalog, IEsp32Transport transport) : IComponentGateway
{
    public async Task<ComponentState> GetStateAsync(string id, CancellationToken cancellationToken) =>
        (await ExchangeAsync("GET_STATE", id, null, cancellationToken)).States[0];

    public async Task<ComponentResult> ExecuteAsync(string id, ComponentCommand command, CancellationToken cancellationToken)
    {
        var wire = new Esp32Command(command.Action!, command.Direction, command.Speed, command.Position);
        var response = await ExchangeAsync("EXECUTE", id, wire, cancellationToken);
        if (!response.States[0].Online) throw new ComponentOperationException("DEVICE_OFFLINE", $"{id}: controlador reportó offline.");
        return new(id, true, response.States[0], response.Time);
    }

    public async Task<IReadOnlyList<ComponentState>> StopAllAndGetStatesAsync(CancellationToken cancellationToken) =>
        (await ExchangeAsync("STOP_ALL", null, null, cancellationToken)).States;

    public async Task StopAllAsync(CancellationToken cancellationToken) => await StopAllAndGetStatesAsync(cancellationToken);

    private async Task<(ComponentState[] States, DateTimeOffset Time)> ExchangeAsync(
        string operation, string? id, Esp32Command? command, CancellationToken ct)
    {
        var request = new Esp32Request(1, Guid.NewGuid(), operation, id, command);
        Esp32Response response;
        try { response = await transport.ExchangeAsync(request, ct); }
        catch (ComponentOperationException error) { throw new ComponentOperationException(error.Code, $"{id ?? "STOP_ALL"}: {error.Code}: {error.Message}"); }
        if (response.Version != 1 || response.RequestId != request.RequestId) throw Invalid(id);
        if (!response.Success)
        {
            var error = response.Error;
            if (error is null || string.IsNullOrWhiteSpace(error.Message) || error.Code is not
                ("DEVICE_OFFLINE" or "TIMEOUT" or "INVALID_COMMAND" or "INVALID_PARAMETER" or "INTERNAL_ERROR")) throw Invalid(id);
            throw new ComponentOperationException(error.Code, $"{id ?? "STOP_ALL"}: {error.Code}: {error.Message}");
        }
        if (response.Error is not null || response.ExecutedAt is null || response.ExecutedAt == default(DateTimeOffset) || response.States is null)
            throw Invalid(id);
        var expected = id is null ? catalog.Components.Select(c => c.Id).ToArray() : new[] { id };
        if (response.States.Length != expected.Length || response.States.Any(s => s is null) ||
            !response.States.Select(s => s.Id).Order().SequenceEqual(expected.Order())) throw Invalid(id);
        var states = response.States.Select(Map).ToArray();
        if (operation == "STOP_ALL" && states.Any(s => !s.Online || s.State is not ("off" or "stopped") ||
            s.Speed != 0 || s.Channels?.Any(on => on) == true || s.Effect is not (null or "NONE") ||
            s.Movement is not (null or "STOPPED"))) throw Invalid(id);
        return (states, response.ExecutedAt.Value.ToUniversalTime());
    }

    // Validate the untrusted wire snapshot before presenting any of it as confirmed.
    private ComponentState Map(Esp32State s)
    {
        var type = catalog.Find(s.Id)?.Type;
        var valid = type switch
        {
            "light" => s.State is "on" or "off",
            "motor" => s.State is "running" or "stopped" && s.Speed is >= 0 and <= 100 &&
                s.Direction is null or "forward" or "reverse" && (s.State != "running" || s.Direction is not null) &&
                (s.State != "stopped" || s.Speed == 0),
            "light_bank" => s.Channels?.Length == 8 && s.EffectSpeed is >= 1 and <= 100 &&
                s.Effect is "NONE" or "SWEEP_RIGHT" or "SWEEP_LEFT" or "PING_PONG" or "BLINK" &&
                s.State == (s.Effect != "NONE" ? "running" : s.Channels.Any(on => on) ? "on" : "off"),
            "HYDRAULIC_ACTUATOR" => s.Position is >= 0 and <= 100 &&
                s.Movement is "STOPPED" or "EXTENDING" or "RETRACTING" &&
                s.State == (s.Movement == "STOPPED" ? "stopped" : "running") &&
                s.LimitExtended == (s.Position == 100) && s.LimitRetracted == (s.Position == 0),
            "SERVO" => s.State == "stopped" && s.Position is >= 0 and <= 180,
            _ => false
        };
        if (!valid || (type != "motor" && (s.Speed != 0 || s.Direction is not null)) ||
            (type != "light_bank" && (s.Channels is not null || s.Effect is not null || s.EffectSpeed is not null)) ||
            (type is not ("SERVO" or "HYDRAULIC_ACTUATOR") && s.Position is not null) ||
            (type != "HYDRAULIC_ACTUATOR" && (s.Movement is not null || s.LimitExtended is not null || s.LimitRetracted is not null))) throw Invalid(s.Id);
        return new(s.Id, s.State, s.Online, s.Direction, s.Speed, s.Channels is null ? null : Array.AsReadOnly(s.Channels),
            s.Effect, s.EffectSpeed, s.Position, s.Movement, s.LimitExtended, s.LimitRetracted);
    }

    private static ComponentOperationException Invalid(string? id) => new("INTERNAL_ERROR", $"{id ?? "STOP_ALL"}: respuesta ESP32 inválida; estado no confirmado.");
}
