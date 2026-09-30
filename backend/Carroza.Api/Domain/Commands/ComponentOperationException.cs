namespace Carroza.Api.Domain.Commands;

public sealed class ComponentOperationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
