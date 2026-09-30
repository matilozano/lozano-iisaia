using Carroza.Api.Application.Services;
using Carroza.Api.Contracts.DTOs;
using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Sequences;
using Microsoft.AspNetCore.Mvc;
namespace Carroza.Api.Controllers;

[ApiController]
[Route("api/sequences")]
public sealed class SequencesController(ISequenceService service) : ControllerBase
{
    /// <summary>Listar definiciones y pasos de las secuencias disponibles.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SequenceDto[]), 200)]
    public IActionResult List() => Ok(service.List().Select(Map));
    /// <summary>Consultar una definición por id (SHOW_FNE).</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SequenceDto), 200)]
    [ProducesResponseType(404)]
    public IActionResult Find(string id) => service.Find(id) is { } sequence ? Ok(Map(sequence)) : NotFound();
    /// <summary>Consultar la ejecución actual o última; IDLE al arrancar.</summary>
    [HttpGet("execution")]
    [ProducesResponseType(typeof(SequenceExecutionDto), 200)]
    public IActionResult Current() => Ok(Map(service.Current()));
    /// <summary>Iniciar ejecución en backend. 409 si hay otra ejecución o una parada en curso.</summary>
    [HttpPost("{id}/start")]
    [ProducesResponseType(typeof(SequenceExecutionDto), 200)]
    [ProducesResponseType(typeof(CommandErrorDto), 409)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Start(string id, CancellationToken ct)
    {
        try { return await service.StartAsync(id, ct) is { } execution ? Ok(Map(execution)) : NotFound(); }
        catch (ComponentOperationException e) { return Conflict(new CommandErrorDto(409, e.Code, e.Message)); }
    }
    /// <summary>Cancelar la secuencia y aplicar parada segura global. Idempotente, incluso sin ejecución activa.</summary>
    [HttpPost("cancel")]
    [ProducesResponseType(typeof(SequenceExecutionDto), 200)]
    public async Task<IActionResult> Cancel() => Ok(Map(await service.CancelAsync()));
    /// <summary>Consultar eventos automáticos posteriores al cursor after (hasta 500 retenidos, sin persistencia).</summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(SequenceEventDto[]), 200)]
    public IActionResult Events([FromQuery] long after = 0) => Ok(service.History(after).Select(e =>
        new SequenceEventDto(e.Id, e.RunId, e.Time, e.Origin, e.ComponentId, Map(e.Command), e.Result)));
    private static DeviceCommandRequest Map(ComponentCommand c) => new(c.Action, c.Direction, c.Speed, c.Position);
    private static SequenceDto Map(SequenceDefinition s) => new(s.Id, s.Name, s.Description,
        s.Steps.Select(p => new SequenceStepDto(p.Order, p.DelayMs, p.ComponentId, Map(p.Command))).ToArray());
    private static SequenceExecutionDto Map(SequenceExecution s) => new(s.RunId, s.SequenceId, s.Status, s.CurrentStep,
        s.TotalSteps, s.StartedAt, s.FinishedAt, s.LastResult, s.Error, s.ElapsedSeconds);
}
