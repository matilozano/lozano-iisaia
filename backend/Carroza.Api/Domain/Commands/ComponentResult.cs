using Carroza.Api.Domain.Components;

namespace Carroza.Api.Domain.Commands;

public record ComponentResult(string ComponentId, bool Success, ComponentState State, DateTimeOffset ExecutedAt);
