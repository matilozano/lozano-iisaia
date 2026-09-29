namespace Carroza.Api.Domain;

public record Component(string Id, string Name, string Type);
public record ComponentCommand(string? Action, string? Direction = null, int? Speed = null);
public record ComponentState(string Id, string State, bool Online, string? Direction = null, int Speed = 0);
public record ComponentResult(string ComponentId, bool Success, ComponentState State, DateTimeOffset ExecutedAt);
