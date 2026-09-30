using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Sequences;
namespace Carroza.Api.Application.Services;

public record SequenceEvent(long Id, Guid RunId, DateTimeOffset Time, string Origin, string ComponentId, ComponentCommand Command, string Result);
public record SequenceExecution(Guid? RunId, string? SequenceId, string Status, int CurrentStep, int TotalSteps,
    DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, string? LastResult, string? Error, double ElapsedSeconds);
public interface ISequenceService
{
    IReadOnlyList<SequenceDefinition> List();
    SequenceDefinition? Find(string id);
    SequenceExecution Current();
    IReadOnlyList<SequenceEvent> History(long after);
    Task<SequenceExecution?> StartAsync(string id, CancellationToken ct);
    Task<SequenceExecution> CancelAsync();
}
public sealed class SequenceService(SequenceCatalog catalog, ComponentCommandExecutor executor,
    IComponentGateway gateway, OperationCoordinator operations, TimeProvider clock, ISequenceDelay? delay = null) : ISequenceService
{
    private readonly object sync = new();
    private SequenceExecution current = new(null, null, "IDLE", 0, 0, null, null, null, null, 0);
    private readonly List<SequenceEvent> events = new();
    private CancellationTokenSource? currentLease;
    private long startedTimestamp;
    private long eventId;
    public IReadOnlyList<SequenceDefinition> List() => catalog.Sequences;
    public SequenceDefinition? Find(string id) => catalog.Find(id);
    public SequenceExecution Current()
    {
        lock (sync)
        {
            if (current.Status == "RUNNING" && currentLease?.IsCancellationRequested == true) Finish("CANCELLED");
            return current.Status == "RUNNING" ? current with { ElapsedSeconds = clock.GetElapsedTime(startedTimestamp).TotalSeconds } : current;
        }
    }
    public IReadOnlyList<SequenceEvent> History(long after) { lock (sync) return events.Where(e => e.Id > after).ToArray(); }
    public async Task<SequenceExecution?> StartAsync(string id, CancellationToken ct)
    {
        var definition = Find(id);
        if (definition is null) return null;
        return await operations.ManualAsync(() =>
        {
            var lease = operations.Begin();
            var runId = Guid.NewGuid();
            SequenceExecution snapshot;
            lock (sync)
            {
                currentLease = lease;
                startedTimestamp = clock.GetTimestamp();
                snapshot = current = new(runId, id, "RUNNING", 0, definition.Steps.Count, clock.GetUtcNow(), null, null, null, 0);
            }
            _ = RunAsync(definition, runId, lease);
            return Task.FromResult<SequenceExecution?>(snapshot);
        }, ct);
    }
    public async Task<SequenceExecution> CancelAsync()
    {
        await operations.StopAsync(() => gateway.StopAllAndGetStatesAsync(CancellationToken.None));
        return Current();
    }
    private async Task RunAsync(SequenceDefinition definition, Guid runId, CancellationTokenSource lease)
    {
        try
        {
            foreach (var step in definition.Steps.OrderBy(step => step.Order))
            {
                await (delay ?? new SequenceDelay(clock)).WaitAsync(step.DelayMs, lease.Token);
                await operations.StepAsync(lease, async () =>
                {
                    lock (sync) if (current.RunId == runId) current = current with { CurrentStep = step.Order };
                    try
                    {
                        var result = await executor.ExecuteAsync(step.ComponentId, step.Command, lease.Token);
                        if (result?.Success != true) throw new ComponentOperationException("COMMAND_FAILED", "Comando sin confirmación.");
                        Record(runId, step, "OK");
                    }
                    catch (OperationCanceledException) when (lease.IsCancellationRequested) { throw; }
                    catch (Exception error)
                    {
                        var code = error is ComponentOperationException componentError ? componentError.Code
                            : error is ArgumentException ? "INVALID_COMMAND" : "INTERNAL_ERROR";
                        Record(runId, step, code);
                        // Keep ownership through safe cleanup; no manual command can interleave.
                        await gateway.StopAllAndGetStatesAsync(CancellationToken.None);
                        lock (sync) if (current.RunId == runId) Finish(lease.IsCancellationRequested ? "CANCELLED" : "FAILED", $"{step.ComponentId}: {code}: {error.Message}");
                        operations.End(lease);
                        throw new SequenceFailedException();
                    }
                });
            }
            await operations.StepAsync(lease, () =>
            {
                lock (sync) if (current.RunId == runId) Finish("COMPLETED");
                operations.End(lease);
                return Task.CompletedTask;
            });
        }
        catch (OperationCanceledException) when (lease.IsCancellationRequested)
        {
            lock (sync) if (current.RunId == runId && current.Status == "RUNNING") Finish("CANCELLED");
        }
        catch (SequenceFailedException) { }
        catch (Exception error)
        {
            lock (sync) if (current.RunId == runId) Finish("FAILED", error.Message);
            operations.End(lease);
        }
    }
    private void Record(Guid runId, SequenceStep step, string result)
    {
        lock (sync)
        {
            events.Add(new(++eventId, runId, clock.GetUtcNow(), "SEQUENCE", step.ComponentId, step.Command, result));
            if (events.Count > 500) events.RemoveAt(0);
            if (current.RunId == runId) current = current with { LastResult = $"{step.ComponentId} / {step.Command.Action} / {result}" };
        }
    }
    // Caller holds sync.
    private void Finish(string status, string? error = null) => current = current with
    {
        Status = status, Error = error, FinishedAt = clock.GetUtcNow(), ElapsedSeconds = clock.GetElapsedTime(startedTimestamp).TotalSeconds
    };
    private sealed class SequenceFailedException : Exception { }
}
