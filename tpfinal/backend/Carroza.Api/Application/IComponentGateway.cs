using Carroza.Api.Domain.Components;
using Carroza.Api.Domain.Commands;

namespace Carroza.Api.Application;

public interface IComponentGateway
{
    Task<Carroza.Api.Domain.Telemetry.TelemetrySnapshot> GetTelemetryAsync(CancellationToken ct) =>
        Task.FromResult(Carroza.Api.Domain.Telemetry.TelemetrySnapshot.Unavailable("UNKNOWN"));
    Task<Carroza.Api.Domain.Telemetry.TelemetrySnapshot> ControlSimulationAsync(Carroza.Api.Domain.Telemetry.SimulationControl control, CancellationToken ct) =>
        throw new ComponentOperationException("INVALID_COMMAND", "Control de simulación no disponible.");
    Task<ComponentState> GetStateAsync(string id, CancellationToken cancellationToken);
    Task<ComponentResult> ExecuteAsync(string id, ComponentCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyList<ComponentState>> StopAllAndGetStatesAsync(CancellationToken cancellationToken);
    Task StopAllAsync(CancellationToken cancellationToken);
}
