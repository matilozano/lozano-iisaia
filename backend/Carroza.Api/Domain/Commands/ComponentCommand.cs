namespace Carroza.Api.Domain.Commands;

public record ComponentCommand(string? Action, string? Direction = null, int? Speed = null);
