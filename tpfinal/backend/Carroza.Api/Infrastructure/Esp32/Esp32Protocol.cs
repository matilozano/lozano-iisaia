using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carroza.Api.Infrastructure.Esp32;

// Wire types are deliberately independent of the public API DTOs and domain models.
public record Esp32Command(string Action, string? Direction = null, int? Speed = null, double? Position = null);
public record Esp32Request(int Version, Guid RequestId, string Operation, string? ComponentId,
    Esp32Command? Command,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ControlToken = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Esp32SimulationControl? Simulation = null);
public record Esp32SimulationControl(string Action, string? Code = null, bool? Active = null);
public record Esp32Alert(string Code, bool Active, bool Acknowledged, DateTimeOffset ActivatedAt, DateTimeOffset? ResolvedAt);
public record Esp32Telemetry(string Source, bool Online, DateTimeOffset? SampledAt, double AgeMs,
    string Quality, double? Voltage, double? Current, double? Temperature, double? MotorSpeed,
    bool ProtectionLatched, bool StopConfirmed, bool CanInjectFaults, Esp32Alert[] Alerts);
public record Esp32State(string Id, string State, bool Online, string? Direction, int Speed,
    bool[]? Channels = null, string? Effect = null, int? EffectSpeed = null,
    double? Position = null, string? Movement = null, bool? LimitExtended = null, bool? LimitRetracted = null);
public record Esp32Error(string Code, string Message);
public record Esp32Response(int Version, Guid RequestId, bool Success, DateTimeOffset? ExecutedAt,
    Esp32State[]? States, Esp32Error? Error, string? ControlToken = null, Esp32Telemetry? Telemetry = null);

public static class Esp32Json
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        RespectRequiredConstructorParameters = true
    };
}

public interface IEsp32Transport
{
    Task<Esp32Response> ExchangeAsync(Esp32Request request, CancellationToken cancellationToken);
}
