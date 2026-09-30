using Carroza.Api.Application.Services;
using Carroza.Api.Domain.Components;
using Carroza.Api.Domain.Commands;
using Carroza.Api.Infrastructure.Gateways;

var catalog = new ComponentCatalog();
var gateway = new SimulatorComponentGateway(catalog, TimeProvider.System);
var service = new ComponentService(catalog, gateway);
var checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
foreach (var item in await service.GetComponentsAsync(default))
    Check(item.State.State is "off" or "stopped" && item.State.Speed == 0, "Estado inicial");
foreach (var id in new[] { "front-lights", "side-lights" })
    foreach (var action in new[] { "on", "off" })
    {
        var result = await service.ExecuteAsync(id, new(action), default);
        Check(result!.Success && result.State.State == action, "Luz no confirmada");
    }
foreach (var direction in new[] { "forward", "reverse" })
    foreach (var speed in new[] { 0, 70, 100 })
    {
        var result = await service.ExecuteAsync("main-motor", new("start", direction, speed), default);
        Check(result!.State.State == "running" && result.State.Speed == speed && result.State.Direction == direction, "Motor incorrecto");
    }
var before = await service.GetStateAsync("main-motor", default);
foreach (var command in new[] { new ComponentCommand("start", Speed: -1), new("start", Speed: 101), new("start", "invalid"), new("stop", Speed: 1), new(null) })
{
    try { await service.ExecuteAsync("main-motor", command, default); throw new Exception("Comando inválido aceptado"); }
    catch (ArgumentException) { checks++; }
    Check(await service.GetStateAsync("main-motor", default) == before, "Comando inválido alteró el estado");
}
Check(await service.GetStateAsync("missing", default) is null, "Componente inexistente");
Check(await service.ExecuteAsync("missing", new("on"), default) is null, "Comando inexistente");
await Task.WhenAll(Enumerable.Range(0, 60).Select(i => Task.Run(async () =>
{
    if (i % 3 == 0) await service.StopAllAsync(default);
    else await service.ExecuteAsync("front-lights", new(i % 2 == 0 ? "on" : "off"), default);
})));
await service.StopAllAsync(new CancellationToken(true));
foreach (var item in await service.GetComponentsAsync(default))
    Check(item.State.State is "off" or "stopped" && item.State.Speed == 0, "Parada general");
Console.WriteLine($"OK: {checks} verificaciones del simulador y servicio; 60 operaciones concurrentes y parada final.");
await LightBankTests.Run();
