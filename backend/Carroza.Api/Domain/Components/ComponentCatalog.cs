namespace Carroza.Api.Domain.Components;

public sealed class ComponentCatalog
{
    public IReadOnlyList<Component> Components { get; } = Array.AsReadOnly(new[]
    {
        new Component("front-lights", "Luces frontales", "light"),
        new Component("side-lights", "Luces laterales", "light"),
        new Component("main-motor", "Motor principal", "motor"),
        new Component("main-light-bank", "Banco de iluminación", "light_bank")
    });

    public Component? Find(string id) => Components.FirstOrDefault(component => component.Id == id);
}
