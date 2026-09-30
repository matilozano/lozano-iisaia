using Carroza.Api.Domain.Commands;
namespace Carroza.Api.Application.Services;

// Serializes mutations. STOP invalidates queued requests before waiting for in-flight work.
public sealed class OperationCoordinator
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly object sync = new();
    private long generation;
    private int stops;
    private CancellationTokenSource? active;

    public async Task<T> ManualAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        long version;
        lock (sync) version = generation;
        await gate.WaitAsync(ct);
        try
        {
            lock (sync)
                if (version != generation || stops > 0 || active is not null)
                    throw new ComponentOperationException("OPERATION_CONFLICT", "Secuencia activa o parada en curso; comando manual rechazado.");
            return await action();
        }
        finally { gate.Release(); }
    }
    // Called inside ManualAsync while admitting a new sequence.
    public CancellationTokenSource Begin()
    {
        lock (sync)
        {
            if (stops > 0) throw new ComponentOperationException("OPERATION_CONFLICT", "Parada en curso.");
            return active = new CancellationTokenSource();
        }
    }
    public async Task StepAsync(CancellationTokenSource lease, Func<Task> action)
    {
        await gate.WaitAsync(lease.Token);
        try
        {
            lock (sync)
            {
                lease.Token.ThrowIfCancellationRequested();
                if (active != lease || stops > 0) throw new OperationCanceledException(lease.Token);
            }
            await action();
        }
        finally { gate.Release(); }
    }
    public void End(CancellationTokenSource lease)
    {
        lock (sync) if (active == lease) active = null;
    }
    public async Task<T> StopAsync<T>(Func<Task<T>> stop)
    {
        lock (sync) { generation++; stops++; active?.Cancel(); }
        await gate.WaitAsync();
        try { return await stop(); }
        finally
        {
            lock (sync) { active = null; stops--; }
            gate.Release();
        }
    }
}
