namespace Carroza.Api.Domain;

public static class CommandValidator
{
    public static void Validate(Component component, ComponentCommand command)
    {
        switch (component.Type)
        {
            case "light":
                if (command.Action is not ("on" or "off") || command.Direction is not null || command.Speed is not null)
                    throw new ArgumentException("Las luces admiten on/off, sin dirección ni velocidad.");
                break;
            case "motor":
                if (command.Action is not ("start" or "stop"))
                    throw new ArgumentException("El motor admite start/stop.");
                if (command.Speed is < 0 or > 100)
                    throw new ArgumentException("La velocidad debe estar entre 0 y 100.");
                if (command.Direction is not null && command.Direction is not ("forward" or "reverse"))
                    throw new ArgumentException("La dirección debe ser forward o reverse.");
                if (command.Action == "stop" && (command.Speed is not null || command.Direction is not null))
                    throw new ArgumentException("stop no admite dirección ni velocidad.");
                break;
            default:
                throw new ArgumentException($"Tipo de componente no soportado: {component.Type}.");
        }
    }
}
