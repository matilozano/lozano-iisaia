namespace Carroza.Api.Contracts.DTOs;

/// <summary>Lectura confirmada. voltage: V, current: A, temperature: °C, motorSpeed: %. Null indica ausencia de lectura; ageMs es antigüedad en ms.</summary>
public record TelemetryDto(string Gateway, string Source, bool Online, DateTimeOffset? SampledAt,
    double AgeMs, string Quality, double? Voltage, double? Current, double? Temperature, double? MotorSpeed,
    bool ProtectionLatched, bool StopConfirmed, bool CanInjectFaults, IReadOnlyList<OperationalAlertDto> Alerts);
/// <summary>Causa, reconocimiento y resolución independientes. Reconocer no elimina una falla activa.</summary>
public record OperationalAlertDto(string Code, bool Active, bool Acknowledged, DateTimeOffset ActivatedAt, DateTimeOffset? ResolvedAt);
/// <summary>Solo simulación: SET_FAULT requiere code y active; ACKNOWLEDGE admite code opcional; RESET no admite parámetros y exige fallas resueltas y reconocidas.</summary>
public record SimulationControlRequest(string Action, string? Code = null, bool? Active = null);
