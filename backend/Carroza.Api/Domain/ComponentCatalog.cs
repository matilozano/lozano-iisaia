namespace Carroza.Api.Domain;

public sealed class ComponentCatalog
{
    public IReadOnlyList<Component> Components { get; } = Array.AsReadOnly(new[]
    {
        new Component("front-lights", "Luces frontales", "light"),
        new Component("side-lights", "Luces laterales", "light"),
        new Component("main-motor", "Motor principal", "motor")
    });

    public Component? Find(string id) => Components.FirstOrDefault(component => component.Id == id);
}
