using Carroza.Api.Domain.Sequences;
namespace Carroza.Api.Application.Services;

public interface ISequenceStore
{
    IReadOnlyList<SequenceDefinition> List();
    SequenceDefinition Save(SequenceDefinition definition, bool create);
    bool Delete(string id, string revision);
    void Reorder(IReadOnlyList<string> ids);
}
