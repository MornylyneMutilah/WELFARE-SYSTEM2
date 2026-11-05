using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.DTOs.Auth;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public record ExternalProviderStatus(string Provider, bool Configured);
public record ExternalStart(string State, string AuthorizationUrl);
public record ExternalPending(string Provider, string Verifier, string Nonce, string Callback, string BrowserHash, int? LinkUserId, int? LinkTokenVersion);
public record ExternalExchange(int UserId, int TokenVersion, string BrowserHash);
public record ExternalCompletion(LoginResponse? Login, bool Linked, string? Error);

public class ExternalAuthService(AppDbContext context, IPasswordHasher passwords, ITokenService tokens,
    IHttpClientFactory clients, ExternalAuthFlowStore store, IGoogleIdTokenValidator google, ExternalAuthSettings settings)
{
    public IReadOnlyList<ExternalProviderStatus> GetProviderStatus() => ExternalAuthSettings.Providers
        .Select(provider => new ExternalProviderStatus(provider, settings.IsConfigured(provider))).ToList();
    public bool IsConfigured(string provider) => settings.IsConfigured(provider);

    public async Task<ExternalStart> StartAsync(string provider, string browserSecret, int? linkUserId = null, string? password = null)
    {
        var canonical = ExternalAuthSettings.Canonical(provider) ?? throw new InvalidOperationException("Unsupported provider.");
        if (!IsConfigured(provider)) throw new InvalidOperationException($"{canonical} sign-in is not configured.");
        int? version = null;
        if (linkUserId.HasValue) {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == linkUserId && u.IsActive);
            if (user == null || string.IsNullOrEmpty(password) || !passwords.VerifyPassword(password, user.PasswordHash))
                throw new UnauthorizedAccessException("Confirm your current account password before linking a provider.");
            version = user.TokenVersion;
        }
        var verifier = ExternalAuthFlowStore.RandomToken();
        var nonce = ExternalAuthFlowStore.RandomToken();
        var callback = settings.Callback(provider);
        var pending = new ExternalPending(canonical.ToLowerInvariant(), verifier, nonce, callback, ExternalAuthFlowStore.Hash(browserSecret), linkUserId, version);
        var state = store.Put(pending, TimeSpan.FromMinutes(10));
        var challenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var query = new Dictionary<string, string> {
            ["response_type"] = "code", ["client_id"] = settings.ClientId(provider), ["redirect_uri"] = callback,
            ["state"] = state, ["code_challenge"] = challenge, ["code_challenge_method"] = "S256"
        };
        var endpoint = canonical switch {
            "Google" => "https://accounts.google.com/o/oauth2/v2/auth",
            "GitHub" => "https://github.com/login/oauth/authorize",
            _ => "https://x.com/i/oauth2/authorize"
        };
        query["scope"] = canonical switch { "Google" => "openid email profile", "GitHub" => "read:user user:email", _ => "users.read tweet.read" };
        if (canonical == "Google") query["nonce"] = nonce;
        return new(state, endpoint + "?" + string.Join("&", query.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value))));
    }

    public async Task<ExternalCompletion> CompleteAsync(string provider, string? code, string? state, string? browserSecret, string? denied = null,
        CancellationToken cancellationToken = default)
    {
        var canonical = ExternalAuthSettings.Canonical(provider)?.ToLowerInvariant();
        if (canonical == null || string.IsNullOrEmpty(browserSecret)) return new(null, false, "Invalid sign-in callback. Start again from this browser.");
        var pending = store.Take<ExternalPending>(state, p => p.Provider == canonical &&
            ExternalAuthFlowStore.Equal(p.BrowserHash, ExternalAuthFlowStore.Hash(browserSecret)));
        if (pending == null) return new(null, false, "Invalid, expired, or already used sign-in state. Start again.");
        if (!string.IsNullOrEmpty(denied)) return new(null, false, "Provider sign-in was cancelled or permission was denied.");
        if (string.IsNullOrWhiteSpace(code) || code.Length > 4096 || !IsConfigured(provider) || pending.Callback != settings.Callback(provider))
            return new(null, false, "The sign-in callback is incomplete or its configuration has changed.");
        try {
            var identity = await ExchangeIdentity(canonical, code, pending, cancellationToken);
            if (identity.Subject.Length is < 1 or > 200) return new(null, false, "The provider returned an invalid identity.");
            var link = await context.ExternalLogins.Include(l => l.User).ThenInclude(u => u.Role)
                .Include(l => l.User).ThenInclude(u => u.Department)
                .FirstOrDefaultAsync(l => l.Provider == canonical && l.ProviderKey == identity.Subject, cancellationToken);
            if (pending.LinkUserId.HasValue) {
                await using var tx = context.Database.CurrentTransaction == null ? await context.Database.BeginTransactionAsync(cancellationToken) : null;
                await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542001)", cancellationToken);
                var owner = await context.Users.FirstOrDefaultAsync(u => u.Id == pending.LinkUserId && u.IsActive && u.TokenVersion == pending.LinkTokenVersion, cancellationToken);
                if (owner == null) return new(null, false, "Your account or session changed. Sign in again before linking.");
                if (link != null && link.UserId != owner.Id) return new(null, false, "This provider identity is already linked to a different account.");
                if (await context.ExternalLogins.AnyAsync(l => l.UserId == owner.Id && l.Provider == canonical && l.ProviderKey != identity.Subject, cancellationToken))
                    return new(null, false, "A different identity from this provider is already linked to your account.");
                if (link == null) context.ExternalLogins.Add(new ExternalLogin {
                    UserId = owner.Id, Provider = canonical, ProviderKey = identity.Subject,
                    Email = identity.Email, EmailVerified = identity.Verified
                });
                context.AuditLogs.Add(new AuditLog { UserId = owner.Id, EntityType = "User", EntityId = owner.Id.ToString(),
                    Action = "SocialProviderLinked", Details = JsonSerializer.Serialize(new { reason = $"Account owner linked {canonical} after password confirmation" }) });
                await context.SaveChangesAsync(cancellationToken);
                if (tx != null) await tx.CommitAsync(cancellationToken);
                return new(null, true, null);
            }
            // Never link accounts or create employee identities based on a matching email.
            if (link == null) return new(null, false, "This provider is not linked. Register or sign in with email, then connect it from Profile.");
            if (!link.User.IsActive) return new(null, false, "This account is suspended. Contact your administrator.");
            return new(BuildLogin(link.User), false, null);
        } catch (Exception error) when (error is HttpRequestException or JsonException or InvalidOperationException or
            Microsoft.IdentityModel.Tokens.SecurityTokenException or DbUpdateException or TaskCanceledException or KeyNotFoundException or ArgumentException) {
            return new(null, false, "The provider could not complete sign-in. Please try again or use email and password.");
        }
    }

    public string StoreExchange(LoginResponse login, string browserSecret) => store.Put(
        new ExternalExchange(login.User.Id, ReadVersion(login.Token), ExternalAuthFlowStore.Hash(browserSecret)), TimeSpan.FromSeconds(60));

    public async Task<LoginResponse?> ConsumeExchangeAsync(string? code, string? browserSecret)
    {
        if (string.IsNullOrEmpty(browserSecret)) return null;
        var exchange = store.Take<ExternalExchange>(code, e => ExternalAuthFlowStore.Equal(e.BrowserHash, ExternalAuthFlowStore.Hash(browserSecret)));
        if (exchange == null) return null;
        var user = await context.Users.Include(u => u.Role).Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == exchange.UserId && u.IsActive && u.TokenVersion == exchange.TokenVersion);
        return user == null ? null : BuildLogin(user);
    }

    private async Task<ProviderIdentity> ExchangeIdentity(string provider, string code, ExternalPending pending, CancellationToken ct)
    {
        var client = clients.CreateClient("ExternalAuth");
        var form = new Dictionary<string, string> {
            ["client_id"] = settings.ClientId(provider), ["code"] = code, ["redirect_uri"] = pending.Callback,
            ["code_verifier"] = pending.Verifier, ["grant_type"] = "authorization_code"
        };
        if (provider != "x") form["client_secret"] = settings.ClientSecret(provider);
        var tokenUrl = provider switch { "google" => "https://oauth2.googleapis.com/token", "github" => "https://github.com/login/oauth/access_token", _ => "https://api.x.com/2/oauth2/token" };
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl) { Content = new FormUrlEncodedContent(form) };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (provider == "x") request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Uri.EscapeDataString(settings.ClientId(provider))}:{Uri.EscapeDataString(settings.ClientSecret(provider))}")));
        var token = await SendJson(client, request, ct);
        if (token.TryGetProperty("error", out _)) throw new InvalidOperationException("Provider denied token exchange.");
        if (provider == "google") {
            var claims = await google.ValidateAsync(RequiredString(token, "id_token"), settings.ClientId(provider), pending.Nonce, ct);
            return new(claims.FindFirstValue("sub")!, claims.FindFirstValue("email"), string.Equals(claims.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase));
        }
        var access = RequiredString(token, "access_token");
        if (!string.Equals(RequiredString(token, "token_type"), "bearer", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unexpected token type.");
        if (provider == "github") {
            var user = await GetUserJson(client, "https://api.github.com/user", access, ct);
            if (!user.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number) throw new InvalidOperationException("Missing provider identity.");
            var emails = await GetUserJson(client, "https://api.github.com/user/emails", access, ct);
            var primary = emails.EnumerateArray().FirstOrDefault(e => e.TryGetProperty("primary", out var p) && p.GetBoolean());
            var email = primary.ValueKind == JsonValueKind.Object && primary.TryGetProperty("email", out var address) ? address.GetString() : null;
            var verified = primary.ValueKind == JsonValueKind.Object && primary.TryGetProperty("verified", out var v) && v.ValueKind == JsonValueKind.True;
            return new(id.ToString(), email, verified);
        }
        var xUser = (await GetUserJson(client, "https://api.x.com/2/users/me", access, ct)).GetProperty("data");
        return new(RequiredString(xUser, "id"), null, false);
    }

    private static async Task<JsonElement> GetUserJson(HttpClient client, string url, string access, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
        request.Headers.UserAgent.ParseAdd("WelfareSystem");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return await SendJson(client, request, ct);
    }
    private static async Task<JsonElement> SendJson(HttpClient client, HttpRequestMessage request, CancellationToken ct)
    {
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return json.RootElement.Clone();
    }
    private static string RequiredString(JsonElement json, string name)
    {
        if (!json.TryGetProperty(name, out var field) || field.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(field.GetString()) || field.GetString()!.Length > 16384)
            throw new InvalidOperationException("Missing provider response field.");
        return field.GetString()!;
    }
    private static int ReadVersion(string token) => int.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
        .ReadJwtToken(token).Claims.First(c => c.Type == "ver").Value);
    private LoginResponse BuildLogin(User user) => new() {
        Token = tokens.GenerateAccessToken(user.Id, user.Email, user.Role.Name, user.TokenVersion),
        RefreshToken = tokens.GenerateRefreshToken(user.Id, user.TokenVersion),
        User = new UserDto { Id = user.Id, MustChangePassword = user.MustChangePassword, Email = user.Email, FirstName = user.FirstName, LastName = user.LastName,
            EmployeeId = user.EmployeeId, Role = user.Role.Name, Department = user.Department.Name }
    };
    private record ProviderIdentity(string Subject, string? Email, bool Verified);
}
