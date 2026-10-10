using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carroza.Api.Infrastructure.Esp32;

// Wire types are deliberately independent of the public API DTOs and domain models.
public record Esp32Command(string Action, string? Direction = null, int? Speed = null, double? Position = null);
public record Esp32Request(int Version, Guid RequestId, string Operation, string? ComponentId,
    Esp32Command? Command,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ControlToken = null);
public record Esp32State(string Id, string State, bool Online, string? Direction, int Speed,
    bool[]? Channels = null, string? Effect = null, int? EffectSpeed = null,
    double? Position = null, string? Movement = null, bool? LimitExtended = null, bool? LimitRetracted = null);
public record Esp32Error(string Code, string Message);
public record Esp32Response(int Version, Guid RequestId, bool Success, DateTimeOffset? ExecutedAt,
    Esp32State[]? States, Esp32Error? Error, string? ControlToken = null);

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
