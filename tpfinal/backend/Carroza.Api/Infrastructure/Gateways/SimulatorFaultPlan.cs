using Carroza.Api.Domain.Commands;

namespace Carroza.Api.Infrastructure.Gateways;

// Startup-only fault plan. Deliberately independent of normal component behavior.
public sealed class SimulatorFaultPlan
{
    private readonly IReadOnlyDictionary<string, string> faults;
    public SimulatorFaultPlan(IReadOnlyDictionary<string, string>? faults = null)
    {
        this.faults = new Dictionary<string, string>(faults ?? new Dictionary<string, string>());
        foreach (var code in this.faults.Values)
            if (code is not ("DEVICE_OFFLINE" or "TIMEOUT" or "INVALID_COMMAND" or "INVALID_PARAMETER" or "INTERNAL_ERROR"))
                throw new ArgumentException($"Falla simulada desconocida: {code}");
    }
    public bool IsOnline(string id) => !faults.TryGetValue(id, out var code) || code != "DEVICE_OFFLINE";
    public void BeforeCommand(string id)
    {
        if (faults.TryGetValue(id, out var code))
            throw new ComponentOperationException(code, $"{id}: falla simulada {code}. El comando no se ejecutó.");
    }
}
