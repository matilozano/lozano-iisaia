using System.Net.Http.Json;
using System.Text.Json;
using Carroza.Api.Domain.Commands;

namespace Carroza.Api.Infrastructure.Esp32;

// No retries: a lost acknowledgment must never execute a command twice.
public sealed class HttpEsp32Transport(HttpClient client, Esp32Options options) : IEsp32Transport
{
    public async Task<Esp32Response> ExchangeAsync(Esp32Request request, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.TimeoutMs);
        try
        {
            using var response = await client.PostAsJsonAsync(options.Endpoint, request, Esp32Json.Options, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw Failure(response.StatusCode switch
                {
                    System.Net.HttpStatusCode.ServiceUnavailable => "DEVICE_OFFLINE",
                    System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.GatewayTimeout => "TIMEOUT",
                    _ => "INTERNAL_ERROR"
                }, $"HTTP {(int)response.StatusCode}");
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw Failure("INTERNAL_ERROR", "Se esperaba application/json.");
            return await response.Content.ReadFromJsonAsync<Esp32Response>(Esp32Json.Options, timeout.Token)
                ?? throw Failure("INTERNAL_ERROR", "Respuesta vacía.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw Failure("TIMEOUT", "Plazo de comunicación agotado; ejecución no confirmada."); }
        catch (HttpRequestException) { throw Failure("DEVICE_OFFLINE", "No se pudo contactar al controlador."); }
        catch (JsonException) { throw Failure("INTERNAL_ERROR", "Respuesta JSON inválida o incompleta."); }
    }

    private static ComponentOperationException Failure(string code, string message) => new(code, $"ESP32: {message}");
}
