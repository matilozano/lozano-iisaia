namespace Carroza.Api.Domain.Components;

public record ComponentState(string Id, string State, bool Online, string? Direction = null, int Speed = 0);
