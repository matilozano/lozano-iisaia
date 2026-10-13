using System.Collections.Concurrent;
using Carroza.Api.Application.Services;
using Carroza.Api.Application;
using Carroza.Api.Domain.Telemetry;
using Carroza.Api.Domain.Components;
using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Sequences;
using Carroza.Api.Infrastructure.Sequences;
using Carroza.Api.Infrastructure.Gateways;

internal static class CustomSequenceTests
{
    public static async Task Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "carroza-sequences-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "sequences.json");
        var checks = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
        void Reject(Action action, string code)
        {
            try { action(); throw new Exception("Aceptó operación inválida: " + code); }
            catch (ComponentOperationException e) { Check(e.Code == code, e.Message); }
        }
        async Task Until(Func<bool> condition)
        {
            for (var i = 0; i < 400 && !condition(); i++) await Task.Delay(5);
            Check(condition(), "Condición de secuencia no alcanzada");
        }
        try
        {
            var catalog = new ComponentCatalog();
            var store = new JsonSequenceStore(path, catalog);
            var definition = new SequenceDefinition("custom", "Prueba", "", new[] {
                new SequenceStep(1, 0, "front-lights", new("on"), "first"),
                new SequenceStep(2, 10, "front-lights", new("off"), "second") });
            var saved = store.Save(definition, true);
            Check(new JsonSequenceStore(path, catalog).List().Single().Revision == saved.Revision, "Persistencia tras recrear almacenamiento");
            Reject(() => store.Save(definition, true), "OPERATION_CONFLICT");
            Reject(() => store.Save(definition, false), "OPERATION_CONFLICT");
            var changed = store.Save(saved with { Name = "Editada" }, false);
            Check(changed.Revision != saved.Revision, "Revisión no avanzó");
            Reject(() => store.Delete(saved.Id, saved.Revision!), "OPERATION_CONFLICT");
            foreach (var invalid in new[] {
                definition with { Name = " " }, definition with { Id = "SHOW_FNE" }, definition with { Id = "execution" },
                definition with { Steps = Array.Empty<SequenceStep>() },
                definition with { Steps = new[] { definition.Steps[0] with { DelayMs = -1 } } },
                definition with { Steps = new[] { definition.Steps[0] with { DelayMs = 600001 } } },
                definition with { Steps = new[] { definition.Steps[0] with { ComponentId = "unknown" } } },
                definition with { Steps = new[] { definition.Steps[0] with { Command = new("start", Speed: 101) } } },
                definition with { Steps = new[] { definition.Steps[0] with { Order = 2 } } },
                definition with { Steps = new[] { definition.Steps[0], definition.Steps[0] with { Order = 2 } } }
            }) Reject(() => store.Save(invalid, true), "INVALID_PARAMETER");
            var copy = store.Save(definition with { Id = "other" }, true);
            store.Reorder(new[] { copy.Id, changed.Id });
            Check(store.List()[0].Id == "other", "Orden persistido");
            Reject(() => store.Reorder(new[] { "other" }), "OPERATION_CONFLICT");
            var concurrent = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(() => {
                try { store.Save(changed with { Name = "Edición " + i }, false); return true; }
                catch (ComponentOperationException e) when (e.Code == "OPERATION_CONFLICT") { return false; }
            })));
            Check(concurrent.Count(ok => ok) == 1, "Ediciones concurrentes perdidas");
            Check(store.Delete(copy.Id, copy.Revision!), "Delete");
            using (var held = new FileStream(path + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Reject(() => new JsonSequenceStore(path, catalog).List(), "OPERATION_CONFLICT");
            var gateway = new SimulatorComponentGateway(catalog, TimeProvider.System);
            var operations = new OperationCoordinator();
            var delay = new ControlledDelay();
            var telemetry = new TelemetryService(gateway, operations, TimeProvider.System);
            var sequence = new SequenceService(new(), new(catalog, gateway), gateway, operations, TimeProvider.System, delay, telemetry, store);
            var definitions = new SequenceDefinitions(store, sequence, operations);
            var components = new ComponentService(catalog, gateway, operations);
            Check(sequence.Current().Status == "IDLE", "Ejecución persistida indebidamente");
            var duplicate = await definitions.DuplicateAsync("SHOW_FNE", default);
            Check(duplicate!.Steps.Count == 14 && duplicate.Steps.All(s => s.Id is not null), "Duplicación SHOW_FNE");
            await sequence.StartAsync("custom", "CONTINUOUS", default);
            await Until(() => delay.Pending.Count == 1);
            try { await definitions.SaveAsync(changed, false, default); throw new Exception("Edición durante ejecución admitida"); }
            catch (ComponentOperationException e) { Check(e.Code == "OPERATION_CONFLICT", "Conflicto durante RUNNING"); }
            delay.Release();
            await Until(() => sequence.Current().CompletedCycles == 1 && delay.Pending.Count == 1);
            Check(sequence.Current().Status == "RUNNING", "Continuo finalizó");
            delay.Release();
            await Until(() => sequence.History(0).Count == 3 && delay.Pending.Count == 1);
            await components.StopAllAndGetStatesAsync(default);
            var count = sequence.History(0).Count;
            delay.Release(); await Task.Delay(30);
            Check(sequence.Current().Status == "CANCELLED" && sequence.History(0).Count == count, "Reactivación después de STOP ALL");
            Check((await gateway.GetStateAsync("front-lights", default))!.State == "off", "Parada insegura");
            await sequence.StartAsync("custom", "CONTINUOUS", default);
            await Until(() => delay.Pending.Count == 1); delay.Release();
            await Until(() => sequence.Current().CompletedCycles == 1 && delay.Pending.Count == 1);
            await sequence.CancelAsync(); delay.Release(); await Task.Delay(30);
            Check(sequence.Current().Status == "CANCELLED" && sequence.Current().CompletedCycles == 1, "Cancelación entre ciclos");
            await sequence.StartAsync("custom", default);
            await Until(() => delay.Pending.Count == 1); delay.Release();
            await Until(() => sequence.Current().Status == "COMPLETED");
            Check(sequence.Current().CompletedCycles == 1, "Ejecución única");
            await sequence.StartAsync("custom", "CONTINUOUS", default);
            await Until(() => delay.Pending.Count == 1);
            await telemetry.ControlAsync(new("SET_FAULT", "COMMUNICATION_LOSS", true), default);
            delay.Release(); await Task.Delay(30);
            Check(sequence.Current().Status == "CANCELLED", "Pérdida de comunicación no canceló");
            Check((await gateway.GetStateAsync("front-lights", default))!.State == "off", "Protección no paró");
            // A definition may change while admission is awaiting telemetry, before the execution lease exists.
            var freshGateway = new SimulatorComponentGateway(catalog, TimeProvider.System);
            var freshOperations = new OperationCoordinator();
            var paused = new PausingTelemetry(freshGateway);
            var freshDelay = new ControlledDelay();
            var freshSequence = new SequenceService(new(), new(catalog, freshGateway), freshGateway, freshOperations, TimeProvider.System, freshDelay, paused, store);
            var edits = new SequenceDefinitions(store, freshSequence, freshOperations);
            var admitting = freshSequence.StartAsync("custom", default);
            await paused.Entered.Task;
            var latest = store.List().Single(s => s.Id == "custom");
            latest = await edits.SaveAsync(latest with { Steps = latest.Steps.Select(s => s.Order == 1 ? s with { Command = new("off") } : s).ToArray() }, false, default);
            paused.Release.SetResult(); await admitting;
            await Until(() => freshDelay.Pending.Count == 1);
            Check((await freshGateway.GetStateAsync("front-lights", default))!.State == "off", "Inicio ejecutó edición obsoleta");
            await freshSequence.CancelAsync(); freshDelay.Release();
            paused = new PausingTelemetry(freshGateway);
            freshSequence = new SequenceService(new(), new(catalog, freshGateway), freshGateway, freshOperations, TimeProvider.System, new ControlledDelay(), paused, store);
            edits = new SequenceDefinitions(store, freshSequence, freshOperations);
            admitting = freshSequence.StartAsync("custom", default);
            await paused.Entered.Task;
            await edits.DeleteAsync(latest.Id, latest.Revision!, default);
            paused.Release.SetResult();
            Check(await admitting is null && freshSequence.Current().Status == "IDLE", "Inicio ejecutó definición eliminada");
            File.WriteAllText(path, "{ corrupto");
            Reject(() => store.List(), "STORAGE_CORRUPT");
            Reject(() => store.Save(definition with { Id = "new" }, true), "STORAGE_CORRUPT");
            Check(File.ReadAllText(path) == "{ corrupto", "Sobrescribió corrupción");
            var occupied = Path.Combine(directory, "occupied.json");
            Directory.CreateDirectory(occupied);
            Reject(() => new JsonSequenceStore(occupied, catalog).Save(definition, true), "STORAGE_ERROR");
            Check(!Directory.EnumerateFiles(directory, "*.tmp").Any(), "Archivo temporal huérfano");
            Console.WriteLine($"OK: {checks} comprobaciones de secuencias personalizadas/persistencia/continuo/seguridad.");
        }
        finally { Directory.Delete(directory, true); }
    }
    private sealed class ControlledDelay : ISequenceDelay
    {
        public ConcurrentQueue<TaskCompletionSource> Pending { get; } = new();
        public Task WaitAsync(int milliseconds, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); if (milliseconds == 0) return Task.CompletedTask;
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Pending.Enqueue(completion); return completion.Task.WaitAsync(ct);
        }
        public void Release() { if (!Pending.TryDequeue(out var item)) throw new Exception("Sin espera"); item.SetResult(); }
    }
    private sealed class PausingTelemetry(IComponentGateway gateway) : ITelemetryService
    {
        public Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<TelemetrySnapshot> ReadAsync(CancellationToken ct)
        { Entered.SetResult(); await Release.Task.WaitAsync(ct); return await gateway.GetTelemetryAsync(ct); }
        public Task<TelemetrySnapshot> ControlAsync(SimulationControl control, CancellationToken ct) => throw new NotSupportedException();
    }
}
