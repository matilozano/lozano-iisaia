using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Telemetry;

namespace Carroza.Api.Infrastructure.Gateways;

// Owned by the simulator gateway; all access under its state lock.
internal sealed class SimulatedTelemetry(TimeProvider clock)
{
    private static readonly string[] codes = ["OVER_TEMPERATURE", "OVER_CURRENT", "VOLTAGE_RANGE", "COMMUNICATION_LOSS", "INVALID_SENSOR", "STALE_SENSOR"];
    private readonly Dictionary<string, OperationalAlert> alerts = new();
    public bool Latched { get; private set; }
    public bool StopConfirmed { get; set; }
    public bool Active(string code) => alerts.TryGetValue(code, out var alert) && alert.Active;

    public void Control(SimulationControl control)
    {
        switch (control.Action)
        {
            case "SET_FAULT" when codes.Contains(control.Code) && control.Active.HasValue:
                if (control.Active.Value)
                {
                    if (!Active(control.Code!)) alerts[control.Code!] = new(control.Code!, true, false, clock.GetUtcNow(), null);
                    Latched = true;
                }
                else if (alerts.TryGetValue(control.Code!, out var alert) && alert.Active)
                    alerts[control.Code!] = alert with { Active = false, ResolvedAt = clock.GetUtcNow() };
                break;
            case "ACKNOWLEDGE" when control.Active is null && (control.Code is null || alerts.ContainsKey(control.Code)):
                foreach (var pair in alerts.ToArray())
                    if (control.Code is null || control.Code == pair.Key) alerts[pair.Key] = pair.Value with { Acknowledged = true };
                break;
            case "RESET" when control.Code is null && control.Active is null:
                if (alerts.Values.Any(a => a.Active || !a.Acknowledged) || (Latched && !StopConfirmed))
                    throw new ComponentOperationException("INVALID_PARAMETER", "Resolver y reconocer fallas; la parada debe estar confirmada antes de restablecer.");
                Latched = false;
                break;
            default: throw new ComponentOperationException("INVALID_PARAMETER", "Acción o falla simulada inválida.");
        }
    }

    public TelemetrySnapshot Read(int speed)
    {
        var offline = Active("COMMUNICATION_LOSS");
        var invalid = Active("INVALID_SENSOR");
        var stale = Active("STALE_SENSOR");
        var quality = offline ? "OFFLINE" : invalid ? "INVALID" : stale ? "STALE" : "VALID";
        return new("SIMULATOR", "SIMULATED", !offline, clock.GetUtcNow().AddMilliseconds(stale ? -5000 : 0), stale ? 5000 : 0, quality,
            offline || invalid ? null : Active("VOLTAGE_RANGE") ? 16 : 24 - speed * .01,
            offline || invalid ? null : Active("OVER_CURRENT") ? 15 : 1 + speed * .08,
            offline || invalid ? null : Active("OVER_TEMPERATURE") ? 85 : 25 + speed * .3,
            offline || invalid ? null : speed, Latched, StopConfirmed, true, alerts.Values.ToArray());
    }
}
