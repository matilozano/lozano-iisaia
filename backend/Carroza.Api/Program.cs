using Carroza.Api.Application;
using Carroza.Api.Application.Services;
using Carroza.Api.Domain.Components;
using Carroza.Api.Infrastructure.Esp32;
using Carroza.Api.Infrastructure.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers(options => options.Filters.Add<ComponentExceptionFilter>())
    .ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true);
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Control de carroza", Version = "v1",
        Description = "API de componentes simulados y secuencias coordinadas. Consulte estados y pruebe comandos o SHOW_FNE. STOP ALL cancela secuencias, apaga iluminación y detiene motor y actuador; conserva el ángulo del servo."
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Carroza.Api.xml"));
    options.OperationFilter<GatewayErrorsOperationFilter>();
});
builder.Services.AddSingleton<ComponentCatalog>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<OperationCoordinator>();
builder.Services.AddSingleton<ComponentCommandExecutor>();
builder.Services.AddSingleton<IComponentService, ComponentService>();
builder.Services.AddSingleton<Carroza.Api.Domain.Sequences.SequenceCatalog>();
builder.Services.AddSingleton<ISequenceService, SequenceService>();
builder.Services.AddHostedService<Carroza.Api.Infrastructure.Lifecycle.SequenceLifetime>();
builder.Services.AddComponentGateway(builder.Configuration);
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Control de carroza v1"));
}
app.MapControllers();
app.Run();
