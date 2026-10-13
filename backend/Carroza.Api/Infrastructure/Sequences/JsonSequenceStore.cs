using System.Text.Json;
using Carroza.Api.Application.Services;
using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Components;
using Carroza.Api.Domain.Sequences;
namespace Carroza.Api.Infrastructure.Sequences;

// Each read/mutation holds an OS file handle; an interrupted write never replaces the last good file.
public sealed class JsonSequenceStore(string path, ComponentCatalog catalog) : ISequenceStore
{
    private readonly object sync = new();
    private readonly string path = Path.GetFullPath(path);
    private sealed record Document(int Version, List<SequenceDefinition> Sequences);
    private T Access<T>(Func<List<SequenceDefinition>, T> action, bool write)
    {
        try { return AccessCore(action, write); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { throw new ComponentOperationException("STORAGE_ERROR", "No se pudo leer o escribir el almacenamiento de secuencias; no se confirmó el cambio."); }
    }
    private T AccessCore<T>(Func<List<SequenceDefinition>, T> action, bool write)
    {
        lock (sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            FileStream fileLock;
            try { fileLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) { throw new ComponentOperationException("OPERATION_CONFLICT", "El almacenamiento está ocupado; vuelva a intentar."); }
            using (fileLock)
            {
                var values = Read();
                var result = action(values);
                if (write)
                {
                    var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    try
                    {
                        using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        { JsonSerializer.Serialize(stream, new Document(1, values)); stream.Flush(true); }
                        if (File.Exists(path)) File.Replace(temp, path, null);
                        else File.Move(temp, path);
                    }
                    finally { if (File.Exists(temp)) File.Delete(temp); }
                }
                return result;
            }
        }
    }
    private List<SequenceDefinition> Read()
    {
        if (!File.Exists(path)) return new();
        try
        {
            var data = JsonSerializer.Deserialize<Document>(File.ReadAllText(path));
            if (data is null || data.Version != 1 || data.Sequences is null || data.Sequences.Select(s => s.Id).Distinct().Count() != data.Sequences.Count) throw new JsonException();
            foreach (var item in data.Sequences)
            { SequenceValidation.Validate(item, catalog); if (string.IsNullOrWhiteSpace(item.Revision)) throw new JsonException(); }
            return data.Sequences;
        }
        catch (Exception e) when (e is JsonException or ComponentOperationException or NullReferenceException)
        { throw new ComponentOperationException("STORAGE_CORRUPT", "Archivo de secuencias inválido. Se conserva sin sobrescribir; restaurar una copia válida."); }
    }
    public IReadOnlyList<SequenceDefinition> List() => Access(values => values.ToArray(), false);
    public SequenceDefinition Save(SequenceDefinition definition, bool create)
    {
        SequenceValidation.Validate(definition, catalog);
        return Access(values =>
        {
            var index = values.FindIndex(s => s.Id == definition.Id);
            if (create ? index >= 0 : index < 0 || values[index].Revision != definition.Revision)
                throw new ComponentOperationException("OPERATION_CONFLICT", "La definición cambió o ya no existe. Recargar antes de guardar.");
            var saved = definition with { Revision = Guid.NewGuid().ToString("N") };
            if (create) values.Add(saved); else values[index] = saved;
            return saved;
        }, true);
    }
    public bool Delete(string id, string revision) => Access(values =>
    {
        var item = values.Find(s => s.Id == id);
        if (item is null) return false;
        if (item.Revision != revision) throw new ComponentOperationException("OPERATION_CONFLICT", "La definición cambió. Recargar antes de eliminar.");
        values.Remove(item); return true;
    }, true);
    public void Reorder(IReadOnlyList<string> ids) => Access(values =>
    {
        if (ids.Count != values.Count || ids.Distinct().Count() != ids.Count || ids.Any(id => values.All(s => s.Id != id)))
            throw new ComponentOperationException("OPERATION_CONFLICT", "El listado cambió; recargar antes de reordenar.");
        var ordered = ids.Select(id => values.Single(s => s.Id == id)).ToArray();
        values.Clear(); values.AddRange(ordered); return true;
    }, true);
}
