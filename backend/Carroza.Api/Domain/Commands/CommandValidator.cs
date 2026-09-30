using Carroza.Api.Domain.Components;

namespace Carroza.Api.Domain.Commands;

public static class CommandValidator
{
    public static void Validate(Component component, ComponentCommand command)
    {
        switch (component.Type)
        {
            case "light_bank":
                if (command.Action is not ("ALL_ON" or "ALL_OFF" or "SWEEP_RIGHT" or "SWEEP_LEFT" or "PING_PONG" or "BLINK" or "STOP_EFFECT" or "SET_SPEED"))
                    throw new ArgumentException("Comando no soportado por el banco de iluminación.");
                if (command.Direction is not null)
                    throw new ArgumentException("El banco no admite direction; utilice SWEEP_RIGHT o SWEEP_LEFT.");
                if (command.Action == "SET_SPEED" ? command.Speed is null or < 1 or > 100 : command.Speed is not null)
                    throw new ArgumentException("Solo SET_SPEED admite speed, obligatorio entre 1 y 100.");
                break;
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
