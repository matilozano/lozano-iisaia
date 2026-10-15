using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Sequences;
namespace Carroza.Api.Application.Services;

public sealed class SequenceDefinitions(ISequenceStore store, ISequenceService sequences, OperationCoordinator operations)
{
    public Task<SequenceDefinition> SaveAsync(SequenceDefinition definition, bool create, CancellationToken ct) =>
        operations.ManualAsync(() => Task.FromResult(store.Save(definition, create)), ct);
    public Task<bool> DeleteAsync(string id, string revision, CancellationToken ct) => operations.ManualAsync(() =>
    {
        if (id == "SHOW_FNE") throw new ComponentOperationException("INVALID_PARAMETER", "SHOW_FNE es inmutable; puede duplicarla.");
        return Task.FromResult(store.Delete(id, revision));
    }, ct);
    public Task<SequenceDefinition?> DuplicateAsync(string id, CancellationToken ct) => operations.ManualAsync(() =>
    {
        var source = sequences.Find(id);
        if (source is null) return Task.FromResult<SequenceDefinition?>(null);
        var copy = source with { Id = Guid.NewGuid().ToString("N"), Name = source.Name.Length > 90 ? source.Name[..90] + " (copia)" : source.Name + " (copia)", Revision = null,
            Steps = source.Steps.Select(s => s with { Id = Guid.NewGuid().ToString("N") }).ToArray() };
        return Task.FromResult<SequenceDefinition?>(store.Save(copy, true));
    }, ct);
    public Task<bool> ReorderAsync(IReadOnlyList<string> ids, CancellationToken ct) => operations.ManualAsync(() =>
    { store.Reorder(ids); return Task.FromResult(true); }, ct);
}
