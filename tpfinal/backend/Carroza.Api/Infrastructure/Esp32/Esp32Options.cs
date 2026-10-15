namespace Carroza.Api.Infrastructure.Esp32;

public sealed class Esp32Options
{
    public string Endpoint { get; set; } = "";
    public int TimeoutMs { get; set; } = 2000;

    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment) || TimeoutMs is < 100 or > 30000)
            throw new InvalidOperationException("ESP32 requiere Endpoint HTTP(S) absoluto sin credenciales/fragmento y TimeoutMs entre 100 y 30000.");
    }
}
