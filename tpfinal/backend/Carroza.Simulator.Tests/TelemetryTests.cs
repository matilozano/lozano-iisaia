using Carroza.Api.Application.Services;
using Carroza.Api.Domain.Components;
using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Telemetry;
using Carroza.Api.Domain.Sequences;
using Carroza.Api.Infrastructure.Gateways;
using Carroza.Api.Application;
using Carroza.Api.Infrastructure.Esp32;

internal static class TelemetryTests
{
    public static async Task Run()
    {
        var checks = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
        async Task Reject(Func<Task> action)
        {
            try { await action(); throw new Exception("Protección permitió operación"); }
            catch (ComponentOperationException) { checks++; }
        }
        var catalog = new ComponentCatalog();
        var gateway = new SimulatorComponentGateway(catalog, TimeProvider.System);
        var operations = new OperationCoordinator();
        var telemetry = new TelemetryService(gateway, operations, TimeProvider.System);
        var sequence = new SequenceService(new SequenceCatalog(), new(catalog, gateway), gateway, operations, TimeProvider.System);
        var normal = await telemetry.ReadAsync(default);
        Check(normal.Voltage == 24 && normal.Current == 1 && normal.Temperature == 25 && normal.Quality == "VALID", "Telemetría inicial");
        await gateway.ExecuteAsync("main-motor", new("start", "forward", 50), default);
        var running = await telemetry.ReadAsync(default);
        Check(running.Voltage == 23.5 && running.Current == 5 && running.Temperature == 40 && running.MotorSpeed == 50, "Evolución determinista");
        foreach (var code in new[] { "OVER_TEMPERATURE", "OVER_CURRENT", "VOLTAGE_RANGE", "COMMUNICATION_LOSS", "INVALID_SENSOR", "STALE_SENSOR" })
        {
            await sequence.StartAsync("SHOW_FNE", default);
            var tripped = await telemetry.ControlAsync(new("SET_FAULT", code, true), default);
            Check(tripped.ProtectionLatched && tripped.Alerts.Any(a => a.Code == code && a.Active), "Falla registrada");
            Check(sequence.Current().Status == "CANCELLED", "Protección cancela secuencia");
            Check((await gateway.GetStateAsync("main-motor", default)).Speed == 0, "Parada confirmada");
            await Reject(() => gateway.ExecuteAsync("front-lights", new("on"), default));
            await Reject(() => sequence.StartAsync("SHOW_FNE", default));
            await Reject(() => telemetry.ControlAsync(new("RESET"), default));
            await telemetry.ControlAsync(new("ACKNOWLEDGE"), default);
            await Reject(() => telemetry.ControlAsync(new("RESET"), default));
            var clear = await telemetry.ControlAsync(new("SET_FAULT", code, false), default);
            Check(clear.ProtectionLatched && clear.Alerts.Single(a => a.Code == code).ResolvedAt is not null, "Resolver no rearma");
            var reset = await telemetry.ControlAsync(new("RESET"), default);
            Check(!reset.ProtectionLatched && sequence.Current().Status == "CANCELLED", "Reset no reanuda");
        }
        var link = new BrokenLink(new SimulatorComponentGateway(catalog, TimeProvider.System));
        var supervision = new TelemetryService(link, operations, TimeProvider.System);
        var healthy = await supervision.ReadAsync(default);
        link.Offline = true;
        var missing = await supervision.ReadAsync(default);
        Check(!missing.Online && missing.Quality == "OFFLINE" && !missing.StopConfirmed && missing.Voltage == healthy.Voltage, "Pérdida real: conservar lectura sin falsa parada");
        await Reject(() => sequence.StartAsync("SHOW_FNE", default));
        link.Offline = false;
        var recovered = await supervision.ReadAsync(default);
        Check(recovered.ProtectionLatched && recovered.Alerts.Any(a => !a.Active && !a.Acknowledged), "Recuperación no rearma");
        await supervision.ControlAsync(new("ACKNOWLEDGE", "COMMUNICATION_LOSS"), default);
        Check(!(await supervision.ControlAsync(new("RESET"), default)).ProtectionLatched, "Restablecer comunicación");

        var wire = new TelemetryTransport();
        var remote = new Esp32ComponentGateway(catalog, wire);
        Check((await remote.GetTelemetryAsync(default)).Gateway == "ESP32", "Gateway remoto real en DTO");
        wire.Unsupported = true;
        Check((await remote.GetTelemetryAsync(default)).Quality == "UNAVAILABLE", "Compatibilidad firmware anterior");
        wire.Unsupported = false; wire.Invalid = true;
        await Reject(() => remote.GetTelemetryAsync(default));
        Console.WriteLine($"OK: {checks} comprobaciones de telemetría/protecciones y secuencias.");
    }

    private sealed class BrokenLink(IComponentGateway inner) : IComponentGateway
    {
        public bool Offline;
        private void Check() { if (Offline) throw new ComponentOperationException("DEVICE_OFFLINE", "Enlace de prueba desconectado"); }
        public Task<TelemetrySnapshot> GetTelemetryAsync(CancellationToken ct) { Check(); return inner.GetTelemetryAsync(ct); }
        public Task<TelemetrySnapshot> ControlSimulationAsync(SimulationControl control, CancellationToken ct) { Check(); return inner.ControlSimulationAsync(control, ct); }
        public Task<ComponentState> GetStateAsync(string id, CancellationToken ct) { Check(); return inner.GetStateAsync(id, ct); }
        public Task<ComponentResult> ExecuteAsync(string id, ComponentCommand command, CancellationToken ct) { Check(); return inner.ExecuteAsync(id, command, ct); }
        public Task<IReadOnlyList<ComponentState>> StopAllAndGetStatesAsync(CancellationToken ct) { Check(); return inner.StopAllAndGetStatesAsync(ct); }
        public Task StopAllAsync(CancellationToken ct) { Check(); return inner.StopAllAsync(ct); }
    }
    private sealed class TelemetryTransport : IEsp32Transport
    {
        public bool Unsupported, Invalid;
        public Task<Esp32Response> ExchangeAsync(Esp32Request request, CancellationToken ct) => Task.FromResult(
            Unsupported ? new Esp32Response(1, request.RequestId, false, null, null, new("INVALID_COMMAND", "Legacy")) :
            new Esp32Response(1, request.RequestId, true, DateTimeOffset.UtcNow, [], null, Telemetry:
                new("SIMULATED", true, DateTimeOffset.UtcNow, 0, "VALID", Invalid ? null : 24, 1, 25, 0, false, false, true, [])));
    }
}
