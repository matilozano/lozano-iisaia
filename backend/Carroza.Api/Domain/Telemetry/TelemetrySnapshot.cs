namespace Carroza.Api.Domain.Telemetry;

public record OperationalAlert(string Code, bool Active, bool Acknowledged, DateTimeOffset ActivatedAt, DateTimeOffset? ResolvedAt);
public record SimulationControl(string Action, string? Code = null, bool? Active = null);
public record TelemetrySnapshot(string Gateway, string Source, bool Online, DateTimeOffset? SampledAt,
    double AgeMs, string Quality, double? Voltage, double? Current, double? Temperature, double? MotorSpeed,
    bool ProtectionLatched, bool StopConfirmed, bool CanInjectFaults, IReadOnlyList<OperationalAlert> Alerts)
{
    public static TelemetrySnapshot Unavailable(string gateway) => new(gateway, "UNAVAILABLE", true, null,
        0, "UNAVAILABLE", null, null, null, null, false, false, false, Array.Empty<OperationalAlert>());
}
