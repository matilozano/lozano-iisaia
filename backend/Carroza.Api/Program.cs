using Carroza.Api.Application;
using Carroza.Api.Application.Services;
using Carroza.Api.Domain.Components;
using Carroza.Api.Infrastructure.Gateways;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true);
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Control de carroza", Version = "v1",
        Description = "API de componentes simulados. Consulte estados y pruebe comandos. STOP ALL apaga iluminación y detiene el motor."
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Carroza.Api.xml"));
});
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Control de carroza v1"));
}
app.MapControllers();
app.Run();
