using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Sequences;
namespace Carroza.Api.Application.Services;

public record SequenceEvent(long Id, Guid RunId, DateTimeOffset Time, string Origin, string ComponentId, ComponentCommand Command, string Result);
public record SequenceExecution(Guid? RunId, string? SequenceId, string Status, int CurrentStep, int TotalSteps,
    DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, string? LastResult, string? Error, double ElapsedSeconds, string Mode = "ONCE", int CompletedCycles = 0);
public interface ISequenceService
{
    IReadOnlyList<SequenceDefinition> List();
    SequenceDefinition? Find(string id);
    SequenceExecution Current();
    IReadOnlyList<SequenceEvent> History(long after);
    Task<SequenceExecution?> StartAsync(string id, CancellationToken ct);
    Task<SequenceExecution?> StartAsync(string id, string mode, CancellationToken ct);
    Task<SequenceExecution> CancelAsync();
}
public sealed class SequenceService(SequenceCatalog catalog, ComponentCommandExecutor executor,
    IComponentGateway gateway, OperationCoordinator operations, TimeProvider clock, ISequenceDelay? delay = null,
    ITelemetryService? telemetry = null, ISequenceStore? store = null) : ISequenceService
{
    private readonly object sync = new();
    private SequenceExecution current = new(null, null, "IDLE", 0, 0, null, null, null, null, 0);
    private readonly List<SequenceEvent> events = new();
    private CancellationTokenSource? currentLease;
    private long startedTimestamp;
    private long eventId;
    public IReadOnlyList<SequenceDefinition> List() => catalog.Sequences.Concat(store?.List() ?? Array.Empty<SequenceDefinition>()).ToArray();
    public SequenceDefinition? Find(string id) => catalog.Find(id) ?? store?.List().FirstOrDefault(s => s.Id == id);
    public SequenceExecution Current()
    {
        lock (sync)
        {
            if (current.Status == "RUNNING" && currentLease?.IsCancellationRequested == true) Finish("CANCELLED");
            return current.Status == "RUNNING" ? current with { ElapsedSeconds = clock.GetElapsedTime(startedTimestamp).TotalSeconds } : current;
        }
    }
    public IReadOnlyList<SequenceEvent> History(long after) { lock (sync) return events.Where(e => e.Id > after).ToArray(); }
    public Task<SequenceExecution?> StartAsync(string id, CancellationToken ct) => StartAsync(id, "ONCE", ct);
    public async Task<SequenceExecution?> StartAsync(string id, string mode, CancellationToken ct)
    {
        if (mode is not ("ONCE" or "CONTINUOUS")) throw new ComponentOperationException("INVALID_PARAMETER", "mode debe ser ONCE o CONTINUOUS.");
        if (Find(id) is null) return null;
        // Admission must not race the first background supervision cycle.
        if (telemetry is not null) await telemetry.ReadAsync(ct);
        return await operations.ManualAsync(() =>
        {
            // CRUD shares this coordinator: capture only after admission, never before an awaited read.
            var definition = Find(id);
            if (definition is null) return Task.FromResult<SequenceExecution?>(null);
            var lease = operations.Begin();
            var runId = Guid.NewGuid();
            SequenceExecution snapshot;
            lock (sync)
            {
                currentLease = lease;
                startedTimestamp = clock.GetTimestamp();
                snapshot = current = new(runId, id, "RUNNING", 0, definition.Steps.Count, clock.GetUtcNow(), null, null, null, 0, mode);
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
            do
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
                        var protectedStop = false;
                        try { protectedStop = (await gateway.GetTelemetryAsync(CancellationToken.None)).ProtectionLatched; }
                        catch (ComponentOperationException) { /* Preserve the original command failure. */ }
                        // Keep ownership through safe cleanup; no manual command can interleave.
                        await gateway.StopAllAndGetStatesAsync(CancellationToken.None);
                        lock (sync) if (current.RunId == runId) Finish(lease.IsCancellationRequested || protectedStop ? "CANCELLED" : "FAILED", $"{step.ComponentId}: {code}: {error.Message}");
                        operations.End(lease);
                        throw new SequenceFailedException();
                    }
                });
            }
            await operations.StepAsync(lease, () =>
            {
                lock (sync) if (current.RunId == runId) current = current with { CompletedCycles = current.CompletedCycles + 1 };
                return Task.CompletedTask;
            });
            bool repeat;
            lock (sync) repeat = current.RunId == runId && current.Mode == "CONTINUOUS";
            if (!repeat) break;
            await (delay ?? new SequenceDelay(clock)).WaitAsync(100, lease.Token);
            // Supervision may itself acquire the coordinator to stop: never call it while holding StepAsync.
            if (telemetry is not null) await telemetry.ReadAsync(lease.Token);
            lease.Token.ThrowIfCancellationRequested();
            } while (true);
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
            operations.End(lease);
        }
        catch (SequenceFailedException) { }
        catch (Exception error)
        {
            try
            {
                await operations.StepAsync(lease, async () =>
                {
                    try { await gateway.StopAllAndGetStatesAsync(CancellationToken.None); }
                    finally { lock (sync) if (current.RunId == runId) Finish("FAILED", error.Message); }
                });
            }
            catch (OperationCanceledException) { lock (sync) if (current.RunId == runId && current.Status == "RUNNING") Finish("CANCELLED"); }
            catch (Exception stopError) { lock (sync) if (current.RunId == runId) Finish("FAILED", $"{error.Message}; parada sin confirmar: {stopError.Message}"); }
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
