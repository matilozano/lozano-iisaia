using Carroza.Api.Domain.Commands;
using Carroza.Api.Application.Services;
using Carroza.Api.Contracts.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Carroza.Api.Controllers;

[ApiController]
[Route("api/devices")]
public sealed class DevicesController(IComponentService service) : ControllerBase
{
    /// <summary>Consultar todos los componentes y sus estados actuales.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(DeviceDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var devices = await service.GetComponentsAsync(cancellationToken);
        return Ok(devices.Select(item => item.ToDto()));
    }

    /// <summary>Consultar el estado de un componente, incluidos los ocho canales del banco.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DeviceStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetState(string id, CancellationToken cancellationToken)
    {
        var state = await service.GetStateAsync(id, cancellationToken);
        return state is null ? NotFound() : Ok(state.ToDto());
    }

    /// <summary>Ejecutar un comando y devolver su estado confirmado.</summary>
    /// <remarks>IDs: front-lights, side-lights, main-motor, main-light-bank, hydraulic-1 (HYDRAULIC_ACTUATOR), servo-1 (SERVO). Hidráulico: { "action": "EXTEND" }, RETRACT o STOP. Servo: { "action": "SET_POSITION", "position": 90 }. Ejemplo motor: { "action": "start", "direction": "forward", "speed": 70 }. Ejemplo banco: { "action": "SWEEP_RIGHT" }. Velocidad banco: { "action": "SET_SPEED", "speed": 50 }.</remarks>
    [HttpPost("{id}/commands")]
    [ProducesResponseType(typeof(DeviceResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommandErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CommandErrorDto), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(CommandErrorDto), StatusCodes.Status504GatewayTimeout)]
    [ProducesResponseType(typeof(CommandErrorDto), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Execute(string id, DeviceCommandRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.ExecuteAsync(id, request.ToCommand(), cancellationToken);
            return result is null ? NotFound() : Ok(result.ToDto());
        }
        catch (ComponentOperationException error)
        {
            var status = error.Code switch { "DEVICE_OFFLINE" => 503, "TIMEOUT" => 504, "INTERNAL_ERROR" => 500, _ => 400 };
            return StatusCode(status, new CommandErrorDto(status, error.Code, error.Message));
        }
        catch (ArgumentException error) { return BadRequest(new CommandErrorDto(400, "INVALID_COMMAND", error.Message)); }
    }

    /// <summary>Apagar luces y todos los canales, cancelar efectos y detener el motor.</summary>
    /// <remarks>204 confirma la parada; includeState=true devuelve 200 con todos los estados tomados bajo el mismo bloqueo. Hidráulico detenido en posición alcanzada; servo conserva ángulo. Banco conserva velocidad; motor queda a 0%.</remarks>
    [HttpPost("stop-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(DeviceDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> StopAll(CancellationToken cancellationToken, [FromQuery] bool includeState = false)
    {
        if (includeState) return Ok((await service.StopAllAndGetStatesAsync(cancellationToken)).Select(item => item.ToDto()));
        await service.StopAllAsync(cancellationToken);
        return NoContent();
    }
}
