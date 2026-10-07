using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Carroza.Api.Application;
using Carroza.Api.Application.Services;
using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Components;
using Carroza.Api.Domain.Sequences;
using Carroza.Api.Infrastructure.Esp32;
using Carroza.Api.Infrastructure.Gateways;
using Carroza.Api.Infrastructure.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static class Esp32Tests
{
    public static async Task Run()
    {
        int checks = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
        async Task Error(string code, Func<Task> action)
        {
            try { await action(); throw new Exception($"Faltó {code}"); }
            catch (ComponentOperationException e) { Check(e.Code == code, $"Se esperaba {code}: {e.Code}"); }
        }
        var catalog = new ComponentCatalog();
        using var controller = new ControllerHandler();
        using var client = new HttpClient(controller);
        var options = new Esp32Options { Endpoint = "http://controller.test/v1/exchange", TimeoutMs = 100 };
        var gateway = new Esp32ComponentGateway(catalog, new HttpEsp32Transport(client, options));
        var service = new ComponentService(catalog, gateway);
        Check((await service.GetComponentsAsync(default)).Count == 6, "Catálogo ESP32");
        Check(controller.Requests.All(r => r.Operation == "GET_STATE" && r.Version == 1 && r.RequestId != Guid.Empty), "Consultas versionadas");
        foreach (var step in new SequenceCatalog().Sequences.Single().Steps)
        {
            var result = await service.ExecuteAsync(step.ComponentId, step.Command, default);
            Check(result!.Success && result.State.Id == step.ComponentId && result.ExecutedAt != default, "Comando SHOW_FNE confirmado");
            var request = controller.Requests.Last();
            Check(request.ComponentId == step.ComponentId && request.Command == new Esp32Command(step.Command.Action!, step.Command.Direction, step.Command.Speed, step.Command.Position), "Traducción de parámetros");
        }
        foreach (var command in new[] { new ComponentCommand("PING_PONG"), new("SET_SPEED", Speed: 100), new("ALL_ON"), new("STOP_EFFECT") })
            Check((await service.ExecuteAsync("main-light-bank", command, default))!.Success, "Banco completo");
        Check((await service.ExecuteAsync("main-motor", new("start", "reverse", 70), default))!.State.Direction == "reverse", "Reversa");
        Check((await service.ExecuteAsync("main-motor", new("start"), default))!.State.Speed == 50, "Defaults del contrato remoto");
        foreach (var position in new[] { 0, 90, 180 })
            Check((await service.ExecuteAsync("servo-1", new("SET_POSITION", Position: position), default))!.State.Position == position, "Servo confirmado");
        var wireJson = controller.Bodies.Last();
        using (var json = JsonDocument.Parse(wireJson))
        {
            Check(json.RootElement.GetProperty("command").GetProperty("position").GetInt32() == 180, "JSON position");
            Check(json.RootElement.GetProperty("command").GetProperty("speed").ValueKind == JsonValueKind.Null, "Parámetros no aplicables nulos");
            Check(json.RootElement.GetProperty("operation").GetString() == "EXECUTE", "JSON operation");
        }
        var stopped = await service.StopAllAndGetStatesAsync(new CancellationToken(true));
        Check(stopped.All(s => s.State.State is "off" or "stopped") && stopped.Single(s => s.Component.Id == "servo-1").State.Position == 180, "STOP ALL y servo conservado");
        Check(controller.Requests.Last().Operation == "STOP_ALL" && controller.Requests.Last().Command is null, "Parada remota única");
        Check(controller.Requests.Select(r => r.RequestId).Distinct().Count() == controller.Requests.Count, "IDs únicos");

        foreach (var code in new[] { "DEVICE_OFFLINE", "TIMEOUT", "INVALID_COMMAND", "INVALID_PARAMETER", "INTERNAL_ERROR" })
        {
            var before = await gateway.GetStateAsync("servo-1", default);
            controller.Code = code;
            await Error(code, () => gateway.ExecuteAsync("servo-1", new("SET_POSITION", Position: 0), default));
            controller.Code = null;
            Check(await gateway.GetStateAsync("servo-1", default) == before, "Error no confirma mutación");
        }
        foreach (var invalid in new[] { "{", "null", "{}", "{\"version\":1}", "<!doctype html>" })
        {
            controller.Raw = invalid;
            await Error("INTERNAL_ERROR", () => gateway.GetStateAsync("servo-1", default));
        }
        controller.Raw = null;
        foreach (var transform in new Func<Esp32Response, Esp32Response>[]
        {
            r => r with { Version = 2 }, r => r with { RequestId = Guid.NewGuid() },
            r => r with { ExecutedAt = null }, r => r with { States = [] },
            r => r with { States = [r.States![0] with { Id = "wrong" }] },
            r => r with { States = [r.States![0] with { Position = 181 }] },
            r => r with { Error = new("INTERNAL_ERROR", "inconsistent") },
            r => r with { Success = false, Error = new("UNKNOWN", "unknown") }
        })
        {
            controller.Transform = transform;
            await Error("INTERNAL_ERROR", () => gateway.GetStateAsync("servo-1", default));
        }
        controller.Transform = r => r with { States = r.States![..1] };
        await Error("INTERNAL_ERROR", () => gateway.StopAllAsync(default));
        controller.Transform = r => r with { States = r.States!.Select(s => s.Id == "main-motor" ? s with { State = "running", Direction = "forward", Speed = 50 } : s).ToArray() };
        await Error("INTERNAL_ERROR", () => gateway.StopAllAsync(default));
        controller.Transform = null;
        foreach (var (id, corrupt) in new (string, Func<Esp32State, Esp32State>)[]
        {
            ("main-motor", s => s with { Speed = 101 }),
            ("main-motor", s => s with { State = "running", Direction = null }),
            ("main-light-bank", s => s with { Channels = [true] }),
            ("main-light-bank", s => s with { Effect = "UNKNOWN" }),
            ("hydraulic-1", s => s with { Position = -1 }),
            ("hydraulic-1", s => s with { Movement = "UNKNOWN" }),
            ("servo-1", s => s with { Channels = new bool[8] })
        })
        {
            controller.Transform = r => r with { States = [corrupt(r.States![0])] };
            await Error("INTERNAL_ERROR", () => gateway.GetStateAsync(id, default));
        }
        controller.Transform = r => r with { States = r.States!.Select(s => s with { Online = false }).ToArray() };
        Check(!(await gateway.GetStateAsync("servo-1", default)).Online, "Offline confirmado en consulta");
        await Error("DEVICE_OFFLINE", () => gateway.ExecuteAsync("servo-1", new("SET_POSITION", Position: 180), default));
        await Error("INTERNAL_ERROR", () => gateway.StopAllAsync(default));
        controller.Transform = null;
        controller.MediaType = "text/html";
        await Error("INTERNAL_ERROR", () => gateway.GetStateAsync("servo-1", default));
        controller.MediaType = "application/json";
        foreach (var (status, code) in new[] { (503, "DEVICE_OFFLINE"), (504, "TIMEOUT"), (408, "TIMEOUT"), (500, "INTERNAL_ERROR"), (302, "INTERNAL_ERROR") })
        {
            controller.Status = status;
            await Error(code, () => gateway.GetStateAsync("servo-1", default));
        }
        controller.Status = 200;
        controller.Offline = true;
        await Error("DEVICE_OFFLINE", () => gateway.GetStateAsync("servo-1", default));
        controller.Offline = false;
        controller.Hang = true;
        var count = controller.Requests.Count;
        await Error("TIMEOUT", () => gateway.ExecuteAsync("servo-1", new("SET_POSITION", Position: 0), default));
        Check(controller.Requests.Count == count + 1, "Sin reintento tras timeout");
        using (var cancel = new CancellationTokenSource(20))
        {
            try { await gateway.GetStateAsync("servo-1", cancel.Token); throw new Exception("Cancelación perdida"); }
            catch (OperationCanceledException) { checks++; }
        }
        controller.Hang = false;
        Check((await gateway.GetStateAsync("servo-1", default)).Position == 180, "Timeout/cancelación sin confirmación falsa");

        // Same Application services with the alternate gateway; no duplicate sequence engine.
        var operations = new OperationCoordinator();
        var sequence = new SequenceService(new(), new(catalog, gateway), gateway, operations, TimeProvider.System, new ImmediateDelay());
        await sequence.StartAsync("SHOW_FNE", default);
        await Until(() => sequence.Current().Status != "RUNNING");
        Check(sequence.Current().Status == "COMPLETED" && sequence.History(0).Count == 14, "SHOW_FNE vía ESP32");
        controller.Code = "TIMEOUT";
        await sequence.StartAsync("SHOW_FNE", default);
        await Until(() => sequence.Current().Status != "RUNNING");
        Check(sequence.Current().Status == "FAILED" && sequence.Current().Error!.Contains("TIMEOUT"), "Fallo de secuencia ESP32");
        controller.Code = null;
        var wait = new BlockingDelay();
        sequence = new(new(), new(catalog, gateway), gateway, operations, TimeProvider.System, wait);
        await sequence.StartAsync("SHOW_FNE", default);
        await wait.Reached.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var components = new ComponentService(catalog, gateway, operations);
        await Error("OPERATION_CONFLICT", () => components.ExecuteAsync("front-lights", new("on"), default));
        await components.StopAllAsync(default);
        Check(sequence.Current().Status == "CANCELLED", "STOP ALL cancela con gateway ESP32");
        var events = sequence.History(0).Count;
        await Task.Delay(30);
        Check(sequence.History(0).Count == events && (await components.GetComponentsAsync(default)).All(c => c.State.State is "off" or "stopped"), "Sin steps posteriores");

        foreach (var mode in new[] { "Simulator", "ESP32" })
        {
            var services = new ServiceCollection();
            services.AddLogging(); services.AddSingleton(catalog); services.AddSingleton(TimeProvider.System);
            services.AddComponentGateway(Config(mode, options.Endpoint));
            using var provider = services.BuildServiceProvider();
            Check(provider.GetRequiredService<IComponentGateway>().GetType() == (mode == "Simulator" ? typeof(SimulatorComponentGateway) : typeof(Esp32ComponentGateway)), "Selección DI");
        }
        foreach (var endpoint in new[] { "", "relative/path", "file:///device", "http://user:pass@device" })
        {
            try { new ServiceCollection().AddComponentGateway(Config("ESP32", endpoint)); throw new Exception("Endpoint inválido admitido"); }
            catch (InvalidOperationException) { checks++; }
        }
        try { new ServiceCollection().AddComponentGateway(Config("unknown", options.Endpoint)); throw new Exception("Modo inválido"); }
        catch (InvalidOperationException) { checks++; }

        // Real HTTP API with the unchanged controllers and a controlled transport.
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers(o => o.Filters.Add<ComponentExceptionFilter>()).AddApplicationPart(typeof(Carroza.Api.Controllers.DevicesController).Assembly);
        builder.Services.AddSingleton(catalog);
        builder.Services.AddSingleton<IComponentGateway>(gateway);
        builder.Services.AddSingleton<IComponentService, ComponentService>();
        await using var app = builder.Build();
        app.MapControllers();
        await app.StartAsync();
        using var api = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        Check((await api.GetAsync("/api/devices")).StatusCode == HttpStatusCode.OK, "Consulta HTTP gateway ESP32");
        foreach (var (code, status) in new[] { ("DEVICE_OFFLINE", 503), ("TIMEOUT", 504), ("INTERNAL_ERROR", 500) })
        {
            controller.Code = code;
            foreach (var path in new[] { "/api/devices/servo-1", "/api/devices/stop-all?includeState=true" })
            {
                using var response = path.Contains("stop-all") ? await api.PostAsync(path, null) : await api.GetAsync(path);
                Check((int)response.StatusCode == status && (await response.Content.ReadAsStringAsync()).Contains(code), "Error de consulta/parada como JSON");
            }
        }
        controller.Code = null;
        Check((await api.PostAsJsonAsync("/api/devices/servo-1/commands", new { action = "SET_POSITION", position = 90 })).StatusCode == HttpStatusCode.OK, "Comando HTTP ESP32");
        Check((await api.PostAsync("/api/devices/stop-all", null)).StatusCode == HttpStatusCode.NoContent, "204 confirmado");
        await app.StopAsync();
        Console.WriteLine($"OK: {checks} verificaciones ESP32 (protocolo, transporte, DI, Application, secuencias y HTTP sin hardware).");
    }

    private static IConfiguration Config(string mode, string endpoint) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    { ["ComponentGateway:Mode"] = mode, ["ComponentGateway:ESP32:Endpoint"] = endpoint }).Build();
    private static async Task Until(Func<bool> condition)
    {
        for (int i = 0; i < 400 && !condition(); i++) await Task.Delay(5);
        if (!condition()) throw new Exception("Secuencia ESP32 no terminó");
    }
    private sealed class ImmediateDelay : ISequenceDelay
    { public Task WaitAsync(int milliseconds, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.CompletedTask; } }
    private sealed class BlockingDelay : ISequenceDelay
    {
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task WaitAsync(int milliseconds, CancellationToken ct)
        {
            if (milliseconds == 0) return Task.CompletedTask;
            Reached.TrySetResult(); return Task.Delay(Timeout.Infinite, ct);
        }
    }
    // Only the test controller executes simulation. Production transport just exchanges JSON.
    private sealed class ControllerHandler : HttpMessageHandler
    {
        private readonly SimulatorComponentGateway simulator = new(new(), TimeProvider.System);
        public List<Esp32Request> Requests { get; } = [];
        public List<string> Bodies { get; } = [];
        public string? Code, Raw;
        public string MediaType = "application/json";
        public int Status = 200;
        public bool Offline, Hang;
        public Func<Esp32Response, Esp32Response>? Transform;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken ct)
        {
            if (message.Method != HttpMethod.Post || message.RequestUri!.AbsoluteUri != "http://controller.test/v1/exchange") throw new Exception("Endpoint/método incorrecto");
            var body = await message.Content!.ReadAsStringAsync(ct);
            Bodies.Add(body);
            var request = JsonSerializer.Deserialize<Esp32Request>(body, Esp32Json.Options)!;
            Requests.Add(request);
            if (Offline) throw new HttpRequestException("offline");
            if (Hang) await Task.Delay(Timeout.Infinite, ct);
            Esp32Response response;
            if (Code is not null) response = new(1, request.RequestId, false, null, null, new(Code, "Falla controlada"));
            else
            {
                var states = request.Operation switch
                {
                    "GET_STATE" => new[] { await simulator.GetStateAsync(request.ComponentId!, ct) },
                    "EXECUTE" => new[] { (await simulator.ExecuteAsync(request.ComponentId!, new(request.Command!.Action, request.Command.Direction, request.Command.Speed, request.Command.Position), ct)).State },
                    "STOP_ALL" => (await simulator.StopAllAndGetStatesAsync(ct)).ToArray(),
                    _ => throw new Exception("Operación desconocida")
                };
                response = new(1, request.RequestId, true, DateTimeOffset.UtcNow,
                    states.Select(s => new Esp32State(s.Id, s.State, s.Online, s.Direction, s.Speed, s.Channels?.ToArray(), s.Effect, s.EffectSpeed, s.Position, s.Movement, s.LimitExtended, s.LimitRetracted)).ToArray(), null);
            }
            if (Transform is not null) response = Transform(response);
            return new((HttpStatusCode)Status) { Content = new StringContent(Raw ?? JsonSerializer.Serialize(response, Esp32Json.Options), Encoding.UTF8, MediaType) };
        }
    }
}

