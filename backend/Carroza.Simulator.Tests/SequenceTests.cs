using System.Collections.Concurrent;
using Carroza.Api.Application.Services;
using Carroza.Api.Domain.Sequences;
using Carroza.Api.Domain.Components;
using Carroza.Api.Infrastructure.Gateways;
using Carroza.Api.Domain.Commands;

internal static class SequenceTests
{
    public static async Task Run()
    {
        int checks = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
        async Task Until(Func<bool> condition)
        {
            for (int i=0; i<400 && !condition(); i++) await Task.Delay(5);
            Check(condition(), "La condición de secuencia no se alcanzó");
        }
        async Task Conflict(Func<Task> action)
        {
            try { await action(); throw new Exception("Faltó 409 de conflicto"); }
            catch (ComponentOperationException e) { Check(e.Code == "OPERATION_CONFLICT", "Código de conflicto"); }
        }
        (SequenceService Sequence, ComponentService Components, ControlledDelay Delay) Create(string? fault = null)
        {
            var catalog = new ComponentCatalog();
            var gateway = new SimulatorComponentGateway(catalog, TimeProvider.System, fault is null ? null :
                new SimulatorFaultPlan(new Dictionary<string,string> { ["main-light-bank"] = fault }));
            var operations = new OperationCoordinator();
            var delays = new ControlledDelay();
            return (new(new SequenceCatalog(), new(catalog, gateway), gateway, operations, TimeProvider.System, delays),
                new(catalog, gateway, operations), delays);
        }
        var setup = Create();
        Check(setup.Sequence.List().Single().Id == "SHOW_FNE", "Listado");
        Check(setup.Sequence.Find("missing") is null, "Secuencia inexistente");
        Check(setup.Sequence.Current().Status == "IDLE", "IDLE inicial");
        Check(await setup.Sequence.StartAsync("missing", default) is null, "Inicio inexistente");
        var started = await setup.Sequence.StartAsync("SHOW_FNE", default);
        Check(started!.Status == "RUNNING" && started.TotalSteps == 14 && started.StartedAt is not null, "Inicio RUNNING");
        await Until(() => setup.Delay.Pending.Count == 1);
        await Conflict(async () => { await setup.Sequence.StartAsync("SHOW_FNE", default); });
        await Conflict(async () => { await setup.Components.ExecuteAsync("main-motor", new("start"), default); });
        // Each delay must be reached only after the preceding command was confirmed.
        foreach (var step in setup.Sequence.Find("SHOW_FNE")!.Steps.Where(s => s.DelayMs > 0))
        {
            await Until(() => setup.Delay.Pending.Count == 1);
            Check(setup.Sequence.History(0).Count == step.Order - 1, "Comando antes del delay");
            Check(setup.Delay.Release() == step.DelayMs, "Delay incorrecto");
        }
        await Until(() => setup.Sequence.Current().Status == "COMPLETED");
        var history = setup.Sequence.History(0);
        var expected = setup.Sequence.Find("SHOW_FNE")!.Steps;
        Check(history.Select(e => (e.ComponentId, e.Command)).SequenceEqual(expected.Select(e => (e.ComponentId, e.Command))), "Orden de steps");
        Check(history.All(e => e.Origin == "SEQUENCE" && e.Result == "OK"), "Historial automático");
        Check(setup.Sequence.Current().FinishedAt is not null && setup.Sequence.Current().CurrentStep == 14, "Finalización");
        Check((await setup.Components.ExecuteAsync("front-lights", new("on"), default))!.Success, "Manual desbloqueado");
        foreach (var global in new[] { false, true })
        {
            setup = Create();
            await setup.Sequence.StartAsync("SHOW_FNE", default);
            await Until(() => setup.Delay.Pending.Count == 1);
            if (global) await setup.Components.StopAllAndGetStatesAsync(default); else await setup.Sequence.CancelAsync();
            Check(setup.Sequence.Current().Status == "CANCELLED", "Cancelación confirmada");
            var count = setup.Sequence.History(0).Count;
            setup.Delay.Release();
            await Task.Delay(30);
            Check(setup.Sequence.History(0).Count == count, "Paso ejecutado tras cancelar");
            Check((await setup.Components.GetComponentsAsync(default)).All(c => c.State.State is "off" or "stopped"), "Parada segura");
            // A stale worker cannot affect a new run.
            Check((await setup.Sequence.StartAsync("SHOW_FNE", default))!.Status == "RUNNING", "Reinicio después de cancelar");
            await setup.Sequence.CancelAsync();
        }
        foreach (var code in new[] { "TIMEOUT", "DEVICE_OFFLINE", "INVALID_COMMAND", "INVALID_PARAMETER", "INTERNAL_ERROR" })
        {
            setup = Create(code);
            await setup.Sequence.StartAsync("SHOW_FNE", default);
            await Until(() => setup.Delay.Pending.Count == 1);
            setup.Delay.Release();
            await Until(() => setup.Sequence.Current().Status == "FAILED");
            Check(setup.Sequence.Current().Error!.Contains(code), "Error perdido");
            Check(setup.Sequence.History(0).Count == 3 && setup.Sequence.History(0).Last().Result == code, "Pasos después de error");
            Check((await setup.Components.GetStateAsync("front-lights", default))!.State == "off", "Luz activa después de error");
        }
        setup = Create();
        async Task<bool> TryStart()
        {
            try { await setup.Sequence.StartAsync("SHOW_FNE", default); return true; }
            catch (ComponentOperationException e) when (e.Code == "OPERATION_CONFLICT") { return false; }
        }
        var starts = await Task.WhenAll(Task.Run(TryStart), Task.Run(TryStart));
        Check(starts.Count(success => success) == 1, "Dos inicios simultáneos admitidos");
        await setup.Sequence.CancelAsync();
        var coordinator = new OperationCoordinator();
        var unblock = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var activeCommand = coordinator.ManualAsync(async () => { await unblock.Task; return true; }, default);
        var queuedCommand = coordinator.ManualAsync(() => Task.FromResult(true), default);
        bool stopped = false;
        var stopping = coordinator.StopAsync(() => { stopped = true; return Task.FromResult(true); });
        Check(!stopped, "STOP no esperó comando en vuelo");
        unblock.SetResult();
        await activeCommand;
        await Conflict(async () => { await queuedCommand; });
        await stopping;
        Check(stopped, "STOP no confirmó");
        Console.WriteLine($"OK: {checks} comprobaciones de secuencias, orden/delays, exclusión, cancelación y cinco fallas.");
    }
    private sealed class ControlledDelay : ISequenceDelay
    {
        public ConcurrentQueue<(int Ms, TaskCompletionSource Completion)> Pending { get; } = new();
        public Task WaitAsync(int milliseconds, CancellationToken ct)
        {
            if (milliseconds == 0) { ct.ThrowIfCancellationRequested(); return Task.CompletedTask; }
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Pending.Enqueue((milliseconds, completion));
            return completion.Task.WaitAsync(ct);
        }
        public int Release()
        {
            if (!Pending.TryDequeue(out var pending)) throw new Exception("No hay delay pendiente");
            pending.Completion.TrySetResult();
            return pending.Ms;
        }
    }
}
