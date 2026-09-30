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
    /// <remarks>IDs: front-lights, side-lights, main-motor, main-light-bank. Ejemplo motor: { "action": "start", "direction": "forward", "speed": 70 }. Ejemplo banco: { "action": "SWEEP_RIGHT" }. Velocidad banco: { "action": "SET_SPEED", "speed": 50 }.</remarks>
    [HttpPost("{id}/commands")]
    [ProducesResponseType(typeof(DeviceResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommandErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Execute(string id, DeviceCommandRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.ExecuteAsync(id, request.ToCommand(), cancellationToken);
            return result is null ? NotFound() : Ok(result.ToDto());
        }
        catch (ArgumentException error) { return BadRequest(new CommandErrorDto(400, "INVALID_COMMAND", error.Message)); }
    }

    /// <summary>Apagar luces y todos los canales, cancelar efectos y detener el motor.</summary>
    /// <remarks>204 confirma la parada. La velocidad configurada del banco se conserva; el motor queda a 0%.</remarks>
    [HttpPost("stop-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StopAll(CancellationToken cancellationToken)
    {
        await service.StopAllAsync(cancellationToken);
        return NoContent();
    }
}
