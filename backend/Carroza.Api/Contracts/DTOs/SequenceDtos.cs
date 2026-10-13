namespace Carroza.Api.Contracts.DTOs;

/// <summary>Espera relativa antes del comando: 0..600000 ms. Id estable obligatorio en definiciones personalizadas; orden consecutivo 1..N.</summary>
public record SequenceStepDto(int Order, int DelayMs, string ComponentId, DeviceCommandRequest Command, string? Id = null, string? Description = null);
/// <summary>Definición guardada. Revision debe reenviarse al actualizar/eliminar; SHOW_FNE no es editable.</summary>
public record SequenceDto(string Id, string Name, string Description, IReadOnlyList<SequenceStepDto> Steps, string? Revision = null);
/// <summary>Status: IDLE, RUNNING, COMPLETED, CANCELLED o FAILED. CurrentStep es el paso intentado; cero antes del primero.</summary>
public record SequenceExecutionDto(Guid? RunId, string? SequenceId, string Status, int CurrentStep, int TotalSteps,
    DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, string? LastResult, string? Error, double ElapsedSeconds, string Mode = "ONCE", int CompletedCycles = 0);
/// <summary>Historial en memoria, hasta 500 eventos. Id creciente durante la vida del proceso; Origin=SEQUENCE.</summary>
public record SequenceEventDto(long Id, Guid RunId, DateTimeOffset Time, string Origin, string ComponentId, DeviceCommandRequest Command, string Result);
