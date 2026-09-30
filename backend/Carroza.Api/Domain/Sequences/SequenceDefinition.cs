using Carroza.Api.Domain.Commands;
namespace Carroza.Api.Domain.Sequences;

public record SequenceStep(int Order, int DelayMs, string ComponentId, ComponentCommand Command);
public record SequenceDefinition(string Id, string Name, string Description, IReadOnlyList<SequenceStep> Steps);
public sealed class SequenceCatalog
{
    public IReadOnlyList<SequenceDefinition> Sequences { get; } = Array.AsReadOnly(new[]
    {
        new SequenceDefinition("SHOW_FNE", "SHOW FNE", "Demostración coordinada de la carroza.", Array.AsReadOnly(new[]
        {
            new SequenceStep(1, 0, "front-lights", new("on")),
            new SequenceStep(2, 0, "side-lights", new("on")),
            new SequenceStep(3, 1000, "main-light-bank", new("SWEEP_RIGHT")),
            new SequenceStep(4, 2000, "main-light-bank", new("SWEEP_LEFT")),
            new SequenceStep(5, 2000, "main-motor", new("start", "forward", 40)),
            new SequenceStep(6, 2000, "hydraulic-1", new("EXTEND")),
            new SequenceStep(7, 2000, "servo-1", new("SET_POSITION", Position: 120)),
            new SequenceStep(8, 2000, "main-light-bank", new("BLINK")),
            new SequenceStep(9, 3000, "hydraulic-1", new("RETRACT")),
            new SequenceStep(10, 2000, "main-motor", new("stop")),
            new SequenceStep(11, 1000, "main-light-bank", new("ALL_OFF")),
            new SequenceStep(12, 0, "front-lights", new("off")),
            new SequenceStep(13, 0, "side-lights", new("off")),
            // Allow retraction to finish before declaring the demonstration complete.
            new SequenceStep(14, 2000, "hydraulic-1", new("STOP"))
        }))
    });
    public SequenceDefinition? Find(string id) => Sequences.FirstOrDefault(s => s.Id == id);
}
