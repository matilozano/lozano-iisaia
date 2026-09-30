using Carroza.Api.Application.Services;
using Carroza.Api.Contracts.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Carroza.Api.Controllers;

[ApiController]
[Route("api/devices")]
public sealed class DevicesController(IComponentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var devices = await service.GetComponentsAsync(cancellationToken);
        return Ok(devices.Select(item => item.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetState(string id, CancellationToken cancellationToken)
    {
        var state = await service.GetStateAsync(id, cancellationToken);
        return state is null ? NotFound() : Ok(state.ToDto());
    }

    [HttpPost("{id}/commands")]
    public async Task<IActionResult> Execute(string id, DeviceCommandRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.ExecuteAsync(id, request.ToCommand(), cancellationToken);
            return result is null ? NotFound() : Ok(result.ToDto());
        }
        catch (ArgumentException error) { return BadRequest(new CommandErrorDto(400, "INVALID_COMMAND", error.Message)); }
    }

    [HttpPost("stop-all")]
    public async Task<IActionResult> StopAll(CancellationToken cancellationToken)
    {
        await service.StopAllAsync(cancellationToken);
        return NoContent();
    }
}
