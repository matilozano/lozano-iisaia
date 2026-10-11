using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Telemetry;

namespace Carroza.Api.Application.Services;

public interface ITelemetryService
{
    Task<TelemetrySnapshot> ReadAsync(CancellationToken ct);
    Task<TelemetrySnapshot> ControlAsync(SimulationControl control, CancellationToken ct);
}

// Serializes supervision and recovery, independently of UI polling.
public sealed class TelemetryService(IComponentGateway gateway, OperationCoordinator operations, TimeProvider clock) : ITelemetryService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private TelemetrySnapshot? last;
    private OperationalAlert? communication;
    private bool stopped;

    public async Task<TelemetrySnapshot> ReadAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try { return await ReadCoreAsync(ct); }
        finally { gate.Release(); }
    }

    private async Task<TelemetrySnapshot> ReadCoreAsync(CancellationToken ct)
    {
        try
        {
            var value = await gateway.GetTelemetryAsync(ct);
            if (communication is { Active: true }) communication = communication with { Active = false, ResolvedAt = clock.GetUtcNow() };
            return await ApplyAsync(value, ct);
        }
        catch (ComponentOperationException error) when (error.Code is "DEVICE_OFFLINE" or "TIMEOUT" or "INTERNAL_ERROR")
        {
            if (communication is not { Active: true }) communication = new("COMMUNICATION_LOSS", true, false, clock.GetUtcNow(), null);
            operations.SetProtection(true);
            var safe = false;
            try { await operations.StopAsync(() => gateway.StopAllAndGetStatesAsync(CancellationToken.None)); safe = true; }
            catch (ComponentOperationException) { /* No fabricated confirmation when transport fails. */ }
            stopped = safe;
            var prior = last ?? TelemetrySnapshot.Unavailable("ESP32");
            return prior with { Online = false, Quality = "OFFLINE", AgeMs = prior.SampledAt is { } time ? Math.Max(0, (clock.GetUtcNow() - time).TotalMilliseconds) : 0,
                ProtectionLatched = true, StopConfirmed = safe, Alerts = prior.Alerts.Where(a => a.Code != "COMMUNICATION_LOSS").Append(communication).ToArray() };
        }
    }

    private async Task<TelemetrySnapshot> ApplyAsync(TelemetrySnapshot value, CancellationToken ct)
    {
        var blocked = value.ProtectionLatched || communication is not null;
        operations.SetProtection(blocked);
        if (blocked && !stopped)
        {
            try { await operations.StopAsync(() => gateway.StopAllAndGetStatesAsync(CancellationToken.None)); stopped = true; }
            catch (ComponentOperationException) { value = value with { StopConfirmed = false }; }
        }
        if (!blocked) stopped = false;
        if (communication is not null) value = value with { ProtectionLatched = true, StopConfirmed = stopped,
            Alerts = value.Alerts.Where(a => a.Code != "COMMUNICATION_LOSS").Append(communication).ToArray() };
        last = value;
        return value;
    }

    public async Task<TelemetrySnapshot> ControlAsync(SimulationControl control, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            // Validation remains in the gateway; no simulator internals in Application.
            if (control.Action == "RESET" && communication is { } alert && (alert.Active || !alert.Acknowledged))
                throw new ComponentOperationException("INVALID_PARAMETER", "Recuperar comunicación y reconocer la alerta antes de restablecer.");
            // A transport alert belongs to Application, not the remote simulator's alert catalog.
            var acknowledgeLocal = control.Action == "ACKNOWLEDGE" && communication is not null &&
                (control.Code is null || control.Code == communication.Code) && control.Active is null;
            var gatewayControl = acknowledgeLocal ? control with { Code = null } : control;
            var value = await gateway.ControlSimulationAsync(gatewayControl, ct);
            if (acknowledgeLocal) communication = communication! with { Acknowledged = true };
            if (control.Action == "RESET") communication = null;
            return await ApplyAsync(value, ct);
        }
        finally { gate.Release(); }
    }
}
