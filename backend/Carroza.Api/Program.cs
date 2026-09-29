using Carroza.Api.Application;
using Carroza.Api.Application.Services;
using Carroza.Api.Domain;
using Carroza.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true);
builder.Services.AddSingleton<ComponentCatalog>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IComponentService, ComponentService>();
var mode = builder.Configuration["ComponentGateway:Mode"] ?? "Simulator";
switch (mode)
{
    case "Simulator":
        builder.Services.AddSingleton<IComponentGateway, SimulatorComponentGateway>();
        break;
    default:
        throw new InvalidOperationException($"Gateway '{mode}' no implementado en la Iteración 1.");
}
var app = builder.Build();
app.MapControllers();
app.Run();
