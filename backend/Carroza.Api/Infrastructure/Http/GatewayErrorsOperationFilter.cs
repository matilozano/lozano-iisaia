using Carroza.Api.Contracts.DTOs;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Carroza.Api.Infrastructure.Http;

public sealed class GatewayErrorsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath;
        if (path is null || !(path.StartsWith("api/devices") || path == "api/sequences/cancel")) return;
        foreach (var (status, description) in new[] { ("500", "INTERNAL_ERROR: respuesta inválida o error del controlador."),
                     ("503", "DEVICE_OFFLINE: comunicación no disponible."), ("504", "TIMEOUT: ejecución/parada no confirmada.") })
            operation.Responses!.TryAdd(status, new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new() { Schema = context.SchemaGenerator.GenerateSchema(typeof(CommandErrorDto), context.SchemaRepository) }
                }
            });
    }
}
