namespace WelfareSystem.Api.Services;

public sealed class ExternalAuthSettings(IConfiguration configuration, IHostEnvironment environment)
{
    public static readonly string[] Providers = ["Google", "GitHub", "X"];
    public static string? Canonical(string? provider) => Providers.FirstOrDefault(p => string.Equals(p, provider, StringComparison.OrdinalIgnoreCase));
    public string FrontendBase => Base("Frontend:BaseUrl", "http://localhost:3000");
    public string BackendBase => Base("Authentication:BackendBaseUrl", "http://localhost:5249");
    public string Callback(string provider)
    {
        var canonical = Canonical(provider) ?? throw new ArgumentException("Unsupported provider.");
        var expected = $"{BackendBase}/api/auth/external/{canonical.ToLowerInvariant()}/callback";
        var configured = configuration[$"Authentication:{canonical}:CallbackUrl"];
        if (!string.IsNullOrWhiteSpace(configured) && !string.Equals(expected, configured, StringComparison.Ordinal))
            throw new InvalidOperationException("The provider callback does not match the configured backend origin and route.");
        return expected;
    }
    public string ClientId(string provider) => configuration[$"Authentication:{Canonical(provider)}:ClientId"] ?? "";
    public string ClientSecret(string provider) => configuration[$"Authentication:{Canonical(provider)}:ClientSecret"] ?? "";
    public bool IsConfigured(string provider)
    {
        if (Canonical(provider) == null || string.IsNullOrWhiteSpace(ClientId(provider)) || string.IsNullOrWhiteSpace(ClientSecret(provider))) return false;
        try { _ = FrontendBase; _ = Callback(provider); return true; } catch (InvalidOperationException) { return false; }
    }
    public bool AllowedOrigin(string? origin) => Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        string.Equals(uri.GetLeftPart(UriPartial.Authority), FrontendBase, StringComparison.OrdinalIgnoreCase) && uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
    private string Base(string key, string developmentDefault)
    {
        var value = configuration[key] ?? (environment.IsDevelopment() ? developmentDefault : "");
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            (uri.Scheme != "https" && !(environment.IsDevelopment() && uri.Scheme == "http" && uri.IsLoopback)))
            throw new InvalidOperationException($"Configure a valid HTTPS origin for {key}; HTTP loopback is allowed only in development.");
        return uri.GetLeftPart(UriPartial.Authority);
    }
}
