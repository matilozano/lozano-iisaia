using Carroza.Api.Application;
using Carroza.Api.Infrastructure.Gateways;

namespace Carroza.Api.Infrastructure.Esp32;

public static class GatewayRegistration
{
    public static IServiceCollection AddComponentGateway(this IServiceCollection services, IConfiguration configuration)
    {
        switch (configuration["ComponentGateway:Mode"] ?? "Simulator")
        {
            case "Simulator":
                services.AddSingleton(new SimulatorFaultPlan(configuration.GetSection("Simulator:Faults").Get<Dictionary<string, string>>()));
                services.AddSingleton<IComponentGateway, SimulatorComponentGateway>();
                break;
            case "ESP32":
                var options = configuration.GetSection("ComponentGateway:ESP32").Get<Esp32Options>() ?? new();
                options.Validate();
                services.AddSingleton(options);
                services.AddHttpClient<IEsp32Transport, HttpEsp32Transport>(client =>
                    { client.Timeout = Timeout.InfiniteTimeSpan; client.MaxResponseContentBufferSize = 65536; })
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
                services.AddSingleton<IComponentGateway, Esp32ComponentGateway>();
                break;
            default:
                throw new InvalidOperationException("ComponentGateway:Mode debe ser Simulator o ESP32.");
        }
        return services;
    }
}
