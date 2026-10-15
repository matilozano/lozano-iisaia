using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Carroza.Api.Contracts.DTOs;

/// <summary>Intención del operador. Luces: on/off. Motor: start/stop. Banco: comandos en mayúsculas.</summary>
public record DeviceCommandRequest(
    [property: Description("Luces: on/off. Motor: start/stop. Banco: ALL_ON, ALL_OFF, SWEEP_RIGHT, SWEEP_LEFT, PING_PONG, BLINK, STOP_EFFECT, SET_SPEED. Hidráulico: EXTEND, RETRACT, STOP. Servo: SET_POSITION.")] string? Action,
    [property: Description("Solo start del motor: forward o reverse; por defecto forward.")] string? Direction = null,
    [property: Description("Motor start: 0..100, por defecto 50. Banco SET_SPEED: obligatorio 1..100. Omitir para otros comandos.")] int? Speed = null,
    [property: Description("Solo servo SET_POSITION: posición angular obligatoria 0..180 grados.")] double? Position = null);

/// <summary>Componente lógico con su último estado confirmado. Campos del banco omitidos para luces y motor.</summary>
public record DeviceDto(string Id, string Name, string Type, string State, bool Online, string? Direction, int Speed,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("Ocho canales, en orden CH01 a CH08. Solo light_bank.")] IReadOnlyList<bool>? Channels = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("NONE, SWEEP_RIGHT, SWEEP_LEFT, PING_PONG o BLINK.")] string? Effect = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("Velocidad del efecto 1..100. Independiente de speed del motor.")] int? EffectSpeed = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("Hidráulico: 0..100%. Servo: 0..180 grados.")] double? Position = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("Solo hidráulico: STOPPED, EXTENDING o RETRACTING.")] string? Movement = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? LimitExtended = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? LimitRetracted = null);

/// <summary>Estado confirmado. STOP_EFFECT conserva el patrón; ALL_OFF y STOP ALL apagan canales.</summary>
public record DeviceStateDto(string Id, string State, bool Online, string? Direction, int Speed,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("Ocho canales CH01..CH08; true significa encendido.")] IReadOnlyList<bool>? Channels = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("Efecto activo o NONE.")] string? Effect = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("Velocidad del efecto 1..100.")] int? EffectSpeed = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("Hidráulico: 0..100%. Servo: 0..180 grados.")] double? Position = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("Solo hidráulico: STOPPED, EXTENDING o RETRACTING.")] string? Movement = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? LimitExtended = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? LimitRetracted = null);
/// <summary>Confirmación de ejecución con estado completo y hora UTC del backend.</summary>
public record DeviceResultDto(string DeviceId, bool Success, DeviceStateDto State, DateTimeOffset ExecutedAt);
/// <summary>Error de comando: INVALID_COMMAND/INVALID_PARAMETER (400), DEVICE_OFFLINE (503), TIMEOUT (504), INTERNAL_ERROR (500). El comando no modifica el componente.</summary>
public record CommandErrorDto(int Status, string Error, string Message);
