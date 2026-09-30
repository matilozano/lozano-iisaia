namespace Carroza.Api.Domain.Components;

public record ComponentState(string Id, string State, bool Online, string? Direction = null, int Speed = 0,
    IReadOnlyList<bool>? Channels = null, string? Effect = null, int? EffectSpeed = null);
