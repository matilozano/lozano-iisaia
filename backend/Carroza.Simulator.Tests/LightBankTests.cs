using Carroza.Api.Application.Services;
using Carroza.Api.Domain.Components;
using Carroza.Api.Domain.Commands;
using Carroza.Api.Infrastructure.Gateways;

internal static class LightBankTests
{
    public static async Task Run()
    {
        var clock = new ManualClock();
        var catalog = new ComponentCatalog();
        var service = new ComponentService(catalog, new SimulatorComponentGateway(catalog, clock));
        const string id = "main-light-bank";
        var checks = 0;
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
        async Task<ComponentState> Read() => (await service.GetStateAsync(id, default))!;
        async Task<ComponentState> Send(string action, int? speed = null) => (await service.ExecuteAsync(id, new(action, Speed: speed), default))!.State;
        void Frame(ComponentState state, int index) => Check(state.Channels!.Count == 8 && state.Channels.Select((on, i) => on == (i == index)).All(equal => equal), "Patrón incorrecto");
        Check((await Read()).Channels!.All(on => !on), "Inicio apagado");
        Check((await Send("ALL_ON")).Channels!.All(on => on), "ALL_ON");
        Check((await Send("ALL_OFF")).Channels!.All(on => !on), "ALL_OFF");
        await Send("SET_SPEED", 100);
        foreach (var effect in new[] { "SWEEP_RIGHT", "SWEEP_LEFT", "PING_PONG" })
        {
            await Send(effect);
            for (var step = 0; step < 16; step++)
            {
                var expected = effect == "SWEEP_RIGHT" ? step % 8 : effect == "SWEEP_LEFT" ? 7 - step % 8 : step % 14 <= 7 ? step % 14 : 14 - step % 14;
                Frame(await Read(), expected);
                clock.Advance(300);
            }
        }
        Check((await Send("BLINK")).Channels!.All(on => on), "BLINK on");
        clock.Advance(300);
        Check((await Read()).Channels!.All(on => !on), "BLINK off");
        clock.Advance(300);
        Check((await Read()).Channels!.All(on => on), "BLINK on otra vez");
        var frozen = await Send("STOP_EFFECT");
        clock.Advance(10000);
        Check((await Read()).Channels!.SequenceEqual(frozen.Channels!) && (await Read()).Effect == "NONE", "STOP_EFFECT congela");
        await Send("SET_SPEED", 1);
        await Send("SWEEP_RIGHT");
        clock.Advance(300);
        Frame(await Read(), 0);
        clock.Advance(891);
        Frame(await Read(), 1);
        await Send("SET_SPEED", 100);
        clock.Advance(300);
        Frame(await Read(), 1);
        await Send("ALL_OFF");
        foreach (var command in new[] { new ComponentCommand("SET_SPEED"), new("SET_SPEED", Speed: 0), new("SET_SPEED", Speed: 101), new("BLINK", Speed: 50), new("ALL_ON", "forward"), new("start") })
        {
            try { await service.ExecuteAsync(id, command, default); throw new Exception("Banco aceptó comando inválido"); }
            catch (ArgumentException) { checks++; }
            Check((await Read()).Channels!.All(on => !on), "Error cambió canales");
        }
        foreach (var effect in new[] { "SWEEP_RIGHT", "SWEEP_LEFT", "PING_PONG", "BLINK", "ALL_ON" })
        {
            await Send(effect);
            await service.StopAllAsync(new CancellationToken(true));
            clock.Advance(10000);
            var stopped = await Read();
            Check(stopped.Effect == "NONE" && stopped.Channels!.All(on => !on), "STOP ALL no canceló efecto");
        }
        Console.WriteLine($"OK: {checks} verificaciones deterministas del banco, tiempo, validaciones y STOP ALL.");
    }
    private sealed class ManualClock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => ticks;
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddMilliseconds(ticks);
        public void Advance(long milliseconds) => ticks += milliseconds;
    }
}
