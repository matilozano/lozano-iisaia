namespace Carroza.Api.Application.Services;

public interface ISequenceDelay
{
    Task WaitAsync(int milliseconds, CancellationToken cancellationToken);
}
public sealed class SequenceDelay(TimeProvider clock) : ISequenceDelay
{
    public Task WaitAsync(int milliseconds, CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromMilliseconds(milliseconds), clock, cancellationToken);
}
