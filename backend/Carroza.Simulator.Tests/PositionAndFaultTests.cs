using Carroza.Api.Application.Services;
using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Components;
using Carroza.Api.Infrastructure.Gateways;

internal static class PositionAndFaultTests
{
    public static async Task Run()
    {
        var clock = new ManualClock();
        var catalog = new ComponentCatalog();
        var service = new ComponentService(catalog, new SimulatorComponentGateway(catalog, clock));
        int checks = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
        async Task<ComponentState> State(string id) => (await service.GetStateAsync(id, default))!;
        async Task<ComponentState> Send(string id, string action, double? position = null) => (await service.ExecuteAsync(id, new(action, Position: position), default))!.State;
        Check((await State("hydraulic-1")).Position == 0, "Hydraulic initial");
        var initial = await Send("hydraulic-1", "EXTEND");
        Check(initial.Position == 0 && initial.Movement == "EXTENDING", "EXTEND teleported");
        Check((await State("hydraulic-1")).Movement == "EXTENDING", "Read cancels movement");
        clock.Advance(1);
        Check((await State("hydraulic-1")).Position == 20, "Progressive extension");
        await Send("hydraulic-1", "STOP"); clock.Advance(1);
        Check((await State("hydraulic-1")).Position == 20 && (await State("hydraulic-1")).Movement == "STOPPED", "STOP drift");
        await Send("hydraulic-1", "RETRACT"); clock.Advance(.5);
        Check((await State("hydraulic-1")).Position == 10, "Progressive retraction");
        clock.Advance(5);
        var lower = await State("hydraulic-1");
        Check(lower.Position == 0 && lower.LimitRetracted == true && lower.Movement == "STOPPED", "Lower limit");
        Check((await Send("hydraulic-1", "RETRACT")).Movement == "STOPPED", "Retraction at limit");
        await Send("hydraulic-1", "EXTEND"); clock.Advance(10);
        var upper = await State("hydraulic-1");
        Check(upper.Position == 100 && upper.LimitExtended == true && upper.Movement == "STOPPED", "Upper limit");
        Check((await Send("hydraulic-1", "EXTEND")).Movement == "STOPPED", "Extension at limit");
        foreach (var angle in new[] { 0, 90, 180 })
            Check((await Send("servo-1", "SET_POSITION", angle)).Position == angle, "Servo angle");
        foreach (var position in new double?[] { null, -1, 181, double.NaN })
        {
            try { await Send("servo-1", "SET_POSITION", position); throw new Exception("Invalid position accepted"); }
            catch (ComponentOperationException e) { Check(e.Code == "INVALID_PARAMETER", "Parameter error code"); }
            Check((await State("servo-1")).Position == 180, "Servo mutated on error");
        }
        try { await Send("hydraulic-1", "SET_POSITION", null); throw new Exception("Invalid action accepted"); }
        catch (ComponentOperationException e) { Check(e.Code == "INVALID_COMMAND", "Command error code"); }
        await Send("servo-1", "SET_POSITION", 90);
        await Send("hydraulic-1", "RETRACT");
        await Send("main-motor", "start"); await Send("main-light-bank", "BLINK"); await Send("front-lights", "on");
        clock.Advance(2);
        var snapshots = await service.StopAllAndGetStatesAsync(new CancellationToken(true));
        Check(snapshots.Single(x => x.Component.Id == "hydraulic-1").State.Position == 60, "STOP ALL snapshot position");
        clock.Advance(10);
        Check((await State("hydraulic-1")).Position == 60 && (await State("hydraulic-1")).Movement == "STOPPED", "STOP ALL hydraulic drift");
        Check((await State("servo-1")).Position == 90, "STOP ALL changed servo");
        Check((await State("main-motor")).Speed == 0 && (await State("front-lights")).State == "off", "STOP ALL previous components");
        Check((await State("main-light-bank")).Channels!.All(x => !x), "STOP ALL bank");
        foreach (var code in new[] { "DEVICE_OFFLINE", "TIMEOUT", "INVALID_COMMAND", "INVALID_PARAMETER", "INTERNAL_ERROR" })
        {
            var failing = new ComponentService(catalog, new SimulatorComponentGateway(catalog, clock,
                new SimulatorFaultPlan(new Dictionary<string, string> { ["servo-1"] = code })));
            var before = await failing.GetStateAsync("servo-1", default);
            try { await failing.ExecuteAsync("servo-1", new("SET_POSITION", Position: 180), default); throw new Exception("Fault succeeded"); }
            catch (ComponentOperationException e) { Check(e.Code == code, "Injected fault code"); }
            Check(await failing.GetStateAsync("servo-1", default) == before, "Fault mutated state");
            Check(before!.Online == (code != "DEVICE_OFFLINE"), "Offline flag");
            Check((await failing.ExecuteAsync("front-lights", new("on"), default))!.Success, "Other component blocked");
            var stopped = await failing.StopAllAndGetStatesAsync(default);
            Check(stopped.Single(x => x.Component.Id == "servo-1").State.Position == 90, "Fault blocked STOP ALL");
        }
        Console.WriteLine($"OK: {checks} pruebas de hidráulico, servo, fallas y STOP ALL con reloj controlado.");
    }
    private sealed class ManualClock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => ticks;
        public void Advance(double seconds) => ticks += (long)(seconds * 1000);
    }
}
