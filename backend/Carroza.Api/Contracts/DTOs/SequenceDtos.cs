namespace Carroza.Api.Contracts.DTOs;

public record SequenceStepDto(int Order, int DelayMs, string ComponentId, DeviceCommandRequest Command);
public record SequenceDto(string Id, string Name, string Description, IReadOnlyList<SequenceStepDto> Steps);
/// <summary>Status: IDLE, RUNNING, COMPLETED, CANCELLED o FAILED. CurrentStep es el paso intentado; cero antes del primero.</summary>
public record SequenceExecutionDto(Guid? RunId, string? SequenceId, string Status, int CurrentStep, int TotalSteps,
    DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, string? LastResult, string? Error, double ElapsedSeconds);
/// <summary>Historial en memoria, hasta 500 eventos. Id creciente durante la vida del proceso; Origin=SEQUENCE.</summary>
public record SequenceEventDto(long Id, Guid RunId, DateTimeOffset Time, string Origin, string ComponentId, DeviceCommandRequest Command, string Result);
