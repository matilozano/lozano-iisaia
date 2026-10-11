using Carroza.Api.Application.Services;
using Carroza.Api.Contracts.DTOs;
using Carroza.Api.Domain.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace Carroza.Api.Controllers;

[ApiController]
[Route("api/telemetry")]
public sealed class TelemetryController(ITelemetryService service) : ControllerBase
{
    /// <summary>Telemetría operativa, modo real de gateway y protecciones; lecturas inválidas/desactualizadas están identificadas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(TelemetryDto), 200)]
    public async Task<TelemetryDto> Read(CancellationToken ct) => Map(await service.ReadAsync(ct));

    /// <summary>Control reproducible de fallas en simulación. No disponible en HAL física. No reanuda movimientos.</summary>
    [HttpPost("simulation")]
    [ProducesResponseType(typeof(TelemetryDto), 200)]
    [ProducesResponseType(typeof(CommandErrorDto), 400)]
    public async Task<TelemetryDto> Control(SimulationControlRequest request, CancellationToken ct) =>
        Map(await service.ControlAsync(new(request.Action, request.Code, request.Active), ct));

    private static TelemetryDto Map(TelemetrySnapshot value) => new(value.Gateway, value.Source, value.Online,
        value.SampledAt, value.AgeMs, value.Quality, value.Voltage, value.Current, value.Temperature, value.MotorSpeed,
        value.ProtectionLatched, value.StopConfirmed, value.CanInjectFaults,
        value.Alerts.Select(a => new OperationalAlertDto(a.Code, a.Active, a.Acknowledged, a.ActivatedAt, a.ResolvedAt)).ToArray());
}
