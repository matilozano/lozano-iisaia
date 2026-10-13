using Carroza.Api.Domain.Commands;
using Carroza.Api.Domain.Components;
namespace Carroza.Api.Domain.Sequences;

public static class SequenceValidation
{
    public static void Validate(SequenceDefinition value, ComponentCatalog catalog)
    {
        void Invalid(string message) => throw new ComponentOperationException("INVALID_PARAMETER", message);
        if (string.IsNullOrWhiteSpace(value.Id) || value.Id.Length > 100 || value.Id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')) || new[] { "SHOW_FNE", "execution", "events", "order", "cancel" }.Contains(value.Id, StringComparer.OrdinalIgnoreCase)) Invalid("Id de secuencia inválido o reservado (letras ASCII, dígitos, guion o guion bajo).");
        if (string.IsNullOrWhiteSpace(value.Name) || value.Name.Length > 100 || value.Description is null || value.Description.Length > 2000) Invalid("Nombre o descripción inválidos.");
        if (value.Steps is null || value.Steps.Count is < 1 or > 200) Invalid("Se requieren entre 1 y 200 pasos.");
        var ids = new HashSet<string>();
        for (var i = 0; i < value.Steps!.Count; i++)
        {
            var step = value.Steps[i];
            if (step is null || string.IsNullOrWhiteSpace(step.Id) || step.Id.Length > 100 || !ids.Add(step.Id) || step.Order != i + 1 || step.DelayMs is < 0 or > 600000 || step.Description?.Length > 500 || step.Command is null)
                Invalid($"Paso {i + 1}: id, orden, espera o descripción inválidos.");
            var component = catalog.Find(step!.ComponentId);
            if (component is null) Invalid($"Paso {i + 1}: componente desconocido.");
            try { CommandValidator.Validate(component!, step.Command!); }
            catch (ArgumentException error) { Invalid($"Paso {i + 1}: {error.Message}"); }
        }
    }
}
