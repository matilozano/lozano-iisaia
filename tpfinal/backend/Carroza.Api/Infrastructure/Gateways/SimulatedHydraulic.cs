using Carroza.Api.Domain.Components;

namespace Carroza.Api.Infrastructure.Gateways;

// Called only while holding the gateway lock. Full travel takes five seconds.
internal sealed class SimulatedHydraulic(TimeProvider clock)
{
    private double position;
    private string movement = "STOPPED";
    private long updated = clock.GetTimestamp();
    public ComponentState Read(string id)
    {
        var now = clock.GetTimestamp();
        var elapsed = clock.GetElapsedTime(updated, now).TotalSeconds;
        updated = now;
        position = Math.Clamp(position + elapsed * (movement == "EXTENDING" ? 20 : movement == "RETRACTING" ? -20 : 0), 0, 100);
        if ((position == 0 && movement == "RETRACTING") || (position == 100 && movement == "EXTENDING")) movement = "STOPPED";
        return new(id, movement == "STOPPED" ? "stopped" : "running", true,
            Position: position, Movement: movement, LimitExtended: position == 100, LimitRetracted: position == 0);
    }
    public ComponentState Execute(string id, string action)
    {
        Read(id);
        movement = action switch { "EXTEND" when position < 100 => "EXTENDING", "RETRACT" when position > 0 => "RETRACTING", _ => "STOPPED" };
        // Do not integrate a second time: command confirmation is the position reached before this command.
        return new(id, movement == "STOPPED" ? "stopped" : "running", true,
            Position: position, Movement: movement, LimitExtended: position == 100, LimitRetracted: position == 0);
    }
}
