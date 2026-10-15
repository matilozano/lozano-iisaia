using Carroza.Api.Application.Services;
using Carroza.Api.Contracts.DTOs;
using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Sequences;
using Microsoft.AspNetCore.Mvc;
namespace Carroza.Api.Controllers;

[ApiController]
[Route("api/sequences")]
public sealed class SequencesController(ISequenceService service, SequenceDefinitions definitions) : ControllerBase
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
    /// <summary>Iniciar en backend: mode=ONCE (predeterminado) o CONTINUOUS. Cada ciclo continuo verifica seguridad antes de repetir. Sin reanudación automática.</summary>
    [HttpPost("{id}/start")]
    [ProducesResponseType(typeof(SequenceExecutionDto), 200)]
    [ProducesResponseType(typeof(CommandErrorDto), 409)]
    [ProducesResponseType(typeof(CommandErrorDto), 400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Start(string id, CancellationToken ct, [FromQuery] string mode = "ONCE")
    {
        return await service.StartAsync(id, mode, ct) is { } execution ? Ok(Map(execution)) : NotFound();
    }
    /// <summary>Crear definición personalizada validada. SHOW_FNE permanece inmutable.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SequenceDto), 201)]
    [ProducesResponseType(typeof(CommandErrorDto), 400)]
    [ProducesResponseType(typeof(CommandErrorDto), 409)]
    public async Task<IActionResult> Create(SequenceDto body, CancellationToken ct)
    {
        var saved = await definitions.SaveAsync(Unmap(body), true, ct);
        return CreatedAtAction(nameof(Find), new { id = saved.Id }, Map(saved));
    }
    /// <summary>Actualizar con revisión optimista; 409 si cambió o hay ejecución activa.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SequenceDto), 200)]
    [ProducesResponseType(typeof(CommandErrorDto), 400)]
    [ProducesResponseType(typeof(CommandErrorDto), 409)]
    public async Task<IActionResult> Update(string id, SequenceDto body, CancellationToken ct)
    {
        if (id != body.Id) throw new ComponentOperationException("INVALID_PARAMETER", "Id de ruta y definición diferentes.");
        return Ok(Map(await definitions.SaveAsync(Unmap(body), false, ct)));
    }
    /// <summary>Eliminar definición con revisión vigente; no elimina SHOW_FNE.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(CommandErrorDto), 409)]
    public async Task<IActionResult> Delete(string id, [FromQuery] string revision, CancellationToken ct) =>
        await definitions.DeleteAsync(id, revision, ct) ? NoContent() : NotFound();
    /// <summary>Duplicar cualquier definición con nuevos identificadores.</summary>
    [HttpPost("{id}/duplicate")]
    [ProducesResponseType(typeof(SequenceDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Duplicate(string id, CancellationToken ct) =>
        await definitions.DuplicateAsync(id, ct) is { } copy ? Ok(Map(copy)) : NotFound();
    /// <summary>Guardar orden de las secuencias personalizadas; SHOW_FNE queda primero.</summary>
    [HttpPost("order")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Reorder([FromBody] string[] ids, CancellationToken ct)
    { await definitions.ReorderAsync(ids, ct); return NoContent(); }
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
        s.Steps.Select(p => new SequenceStepDto(p.Order, p.DelayMs, p.ComponentId, Map(p.Command), p.Id, p.Description)).ToArray(), s.Revision);
    private static SequenceDefinition Unmap(SequenceDto s)
    {
        if (s.Steps is null || s.Steps.Any(p => p is null || p.Command is null))
            throw new ComponentOperationException("INVALID_PARAMETER", "Pasos y comandos son obligatorios.");
        return new(s.Id, s.Name, s.Description, s.Steps.Select(p => new SequenceStep(p.Order, p.DelayMs, p.ComponentId,
            new(p.Command.Action, p.Command.Direction, p.Command.Speed, p.Command.Position), p.Id, p.Description)).ToArray(), s.Revision);
    }
    private static SequenceExecutionDto Map(SequenceExecution s) => new(s.RunId, s.SequenceId, s.Status, s.CurrentStep,
        s.TotalSteps, s.StartedAt, s.FinishedAt, s.LastResult, s.Error, s.ElapsedSeconds, s.Mode, s.CompletedCycles);
}
