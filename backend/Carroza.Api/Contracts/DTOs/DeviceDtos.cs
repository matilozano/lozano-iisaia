namespace Carroza.Api.Contracts.DTOs;

public record DeviceCommandRequest(string? Action, string? Direction = null, int? Speed = null);
public record DeviceDto(string Id, string Name, string Type, string State, bool Online, string? Direction, int Speed);
public record DeviceStateDto(string Id, string State, bool Online, string? Direction, int Speed);
public record DeviceResultDto(string DeviceId, bool Success, DeviceStateDto State, DateTimeOffset ExecutedAt);
public record CommandErrorDto(int Status, string Error, string Message);
