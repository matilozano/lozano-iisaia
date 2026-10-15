using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Components;

namespace Carroza.Api.Infrastructure.Gateways;

// Access is serialized by the gateway lock. No timers or browser-owned state.
internal sealed class SimulatedLightBank(TimeProvider clock)
{
    private string effect = "NONE";
    private int speed = 50;
    private long started = clock.GetTimestamp();
    private IReadOnlyList<bool> held = Array.AsReadOnly(new bool[8]);

    public ComponentState Read(string id)
    {
        var step = (long)(clock.GetElapsedTime(started).TotalMilliseconds / (1200 - 9 * speed));
        var channels = effect switch
        {
            "SWEEP_RIGHT" => Frame((int)(step % 8)),
            "SWEEP_LEFT" => Frame(7 - (int)(step % 8)),
            "PING_PONG" => Frame((int)(step % 14) <= 7 ? (int)(step % 14) : 14 - (int)(step % 14)),
            "BLINK" => Array.AsReadOnly(Enumerable.Repeat(step % 2 == 0, 8).ToArray()),
            _ => held
        };
        return new(id, effect != "NONE" ? "running" : channels.Any(on => on) ? "on" : "off", true,
            Channels: channels, Effect: effect, EffectSpeed: speed);
    }

    public ComponentState Execute(string id, ComponentCommand command)
    {
        switch (command.Action)
        {
            case "ALL_ON":
            case "ALL_OFF":
                effect = "NONE";
                held = Array.AsReadOnly(Enumerable.Repeat(command.Action == "ALL_ON", 8).ToArray());
                break;
            case "STOP_EFFECT":
                held = Read(id).Channels!;
                effect = "NONE";
                break;
            case "SET_SPEED":
                speed = command.Speed!.Value;
                started = clock.GetTimestamp();
                break;
            default:
                effect = command.Action!;
                started = clock.GetTimestamp();
                break;
        }
        return Read(id);
    }

    public void StopAll()
    {
        effect = "NONE";
        held = Array.AsReadOnly(new bool[8]);
    }

    private static IReadOnlyList<bool> Frame(int active) =>
        Array.AsReadOnly(Enumerable.Range(0, 8).Select(channel => channel == active).ToArray());
}
