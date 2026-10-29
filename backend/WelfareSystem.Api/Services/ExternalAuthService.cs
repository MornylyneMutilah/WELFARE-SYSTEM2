using System.Collections.Concurrent;
using System.Net.Http.Headers;
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

public class ExternalAuthService
{
    private static readonly ConcurrentDictionary<string, PendingAuth> Pending = new();
    private static readonly ConcurrentDictionary<string, LoginResponse> Exchanges = new();

    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalAuthService(
        IConfiguration configuration,
        AppDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _httpClientFactory = httpClientFactory;
    }

    public IReadOnlyList<ExternalProviderStatus> GetProviderStatus()
    {
        return new[] { "Google", "GitHub", "X" }
            .Select(p => new ExternalProviderStatus(p, IsConfigured(p)))
            .ToList();
    }

    public bool IsConfigured(string provider)
    {
        var section = ProviderSection(provider);
        return !string.IsNullOrWhiteSpace(section?["ClientId"]) &&
               !string.IsNullOrWhiteSpace(section?["ClientSecret"]);
    }

    public string? BuildAuthorizationUrl(string provider, string callbackUrl)
    {
        if (!IsConfigured(provider))
        {
            return null;
        }

        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(24));
        var nonce = Base64Url(RandomNumberGenerator.GetBytes(16));

        Pending[state] = new PendingAuth(provider.ToLowerInvariant(), verifier, nonce, DateTime.UtcNow.AddMinutes(10));
        Cleanup();

        var clientId = ProviderSection(provider)!["ClientId"];
        var encodedRedirect = Uri.EscapeDataString(callbackUrl);
        var encodedState = Uri.EscapeDataString(state);

        return provider.ToLowerInvariant() switch
        {
            "google" =>
                $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={Uri.EscapeDataString(clientId!)}&redirect_uri={encodedRedirect}&scope={Uri.EscapeDataString("openid email profile")}&state={encodedState}&nonce={nonce}&code_challenge={challenge}&code_challenge_method=S256",
            "github" =>
                $"https://github.com/login/oauth/authorize?client_id={Uri.EscapeDataString(clientId!)}&redirect_uri={encodedRedirect}&scope={Uri.EscapeDataString("read:user user:email")}&state={encodedState}",
            "x" =>
                $"https://twitter.com/i/oauth2/authorize?response_type=code&client_id={Uri.EscapeDataString(clientId!)}&redirect_uri={encodedRedirect}&scope={Uri.EscapeDataString("users.read tweet.read")}&state={encodedState}&code_challenge={challenge}&code_challenge_method=S256",
            _ => null
        };
    }

    public async Task<(LoginResponse? Login, string? Error)> CompleteAsync(string provider, string code, string state, string callbackUrl)
    {
        if (!Pending.TryRemove(state, out var pending) || pending.ExpiresAt < DateTime.UtcNow)
        {
            return (null, "Invalid or expired OAuth state. Start sign-in again.");
        }

        if (!string.Equals(pending.Provider, provider, StringComparison.OrdinalIgnoreCase))
        {
            return (null, "OAuth provider did not match the original request.");
        }

        if (!IsConfigured(provider))
        {
            return (null, $"{provider} is not configured.");
        }

        try
        {
            var identity = await ExchangeCodeAsync(provider, code, pending.Verifier, callbackUrl);
            if (identity == null)
            {
                return (null, "The provider did not return a usable identity.");
            }

            var login = await ResolveUserAsync(provider, identity);
            if (login == null)
            {
                return (null, identity.LinkBlockedReason ?? "Unable to complete social sign-in.");
            }

            return (login, null);
        }
        catch (Exception ex)
        {
            return (null, $"Provider error: {ex.Message}");
        }
    }

    public string StoreExchange(LoginResponse login)
    {
        var code = Base64Url(RandomNumberGenerator.GetBytes(24));
        Exchanges[code] = login;
        return code;
    }

    public LoginResponse? ConsumeExchange(string code)
    {
        Exchanges.TryRemove(code, out var login);
        return login;
    }

    private async Task<ProviderIdentity?> ExchangeCodeAsync(string provider, string code, string verifier, string callbackUrl)
    {
        var client = _httpClientFactory.CreateClient();
        var section = ProviderSection(provider)!;
        var clientId = section["ClientId"]!;
        var clientSecret = section["ClientSecret"]!;

        switch (provider.ToLowerInvariant())
        {
            case "google":
            {
                var token = await PostForm(client, "https://oauth2.googleapis.com/token", new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["redirect_uri"] = callbackUrl,
                    ["grant_type"] = "authorization_code",
                    ["code_verifier"] = verifier
                });
                var access = token.GetProperty("access_token").GetString();
                using var req = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
                var userinfo = JsonDocument.Parse(await (await client.SendAsync(req)).Content.ReadAsStringAsync()).RootElement;
                var email = userinfo.GetProperty("email").GetString();
                var verified = userinfo.TryGetProperty("email_verified", out var v) && v.ValueKind == JsonValueKind.True;
                var sub = userinfo.GetProperty("sub").GetString();
                var given = userinfo.TryGetProperty("given_name", out var gn) ? gn.GetString() : "Google";
                var family = userinfo.TryGetProperty("family_name", out var fn) ? fn.GetString() : "User";
                return new ProviderIdentity(sub!, email, verified, given ?? "Google", family ?? "User");
            }
            case "github":
            {
                using var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["code"] = code,
                        ["redirect_uri"] = callbackUrl
                    })
                };
                tokenReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var tokenJson = JsonDocument.Parse(await (await client.SendAsync(tokenReq)).Content.ReadAsStringAsync()).RootElement;
                var access = tokenJson.GetProperty("access_token").GetString();
                using var userReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
                userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
                userReq.Headers.UserAgent.ParseAdd("WelfareSystem");
                var ghUser = JsonDocument.Parse(await (await client.SendAsync(userReq)).Content.ReadAsStringAsync()).RootElement;
                var id = ghUser.GetProperty("id").ToString();
                var loginName = ghUser.TryGetProperty("login", out var loginEl) ? loginEl.GetString() : "github";
                using var emailReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                emailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
                emailReq.Headers.UserAgent.ParseAdd("WelfareSystem");
                var emails = JsonDocument.Parse(await (await client.SendAsync(emailReq)).Content.ReadAsStringAsync()).RootElement;
                string? email = null;
                var verified = false;
                foreach (var item in emails.EnumerateArray())
                {
                    var isPrimary = item.TryGetProperty("primary", out var p) && p.GetBoolean();
                    var isVerified = item.TryGetProperty("verified", out var ve) && ve.GetBoolean();
                    if (isPrimary)
                    {
                        email = item.GetProperty("email").GetString();
                        verified = isVerified;
                        if (verified) break;
                    }
                }
                return new ProviderIdentity(id, email, verified, loginName ?? "GitHub", "User");
            }
            case "x":
            {
                var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                using var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://api.twitter.com/2/oauth2/token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["code"] = code,
                        ["grant_type"] = "authorization_code",
                        ["redirect_uri"] = callbackUrl,
                        ["code_verifier"] = verifier
                    })
                };
                tokenReq.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
                var tokenJson = JsonDocument.Parse(await (await client.SendAsync(tokenReq)).Content.ReadAsStringAsync()).RootElement;
                var access = tokenJson.GetProperty("access_token").GetString();
                using var userReq = new HttpRequestMessage(HttpMethod.Get, "https://api.twitter.com/2/users/me?user.fields=name,username");
                userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
                var xUser = JsonDocument.Parse(await (await client.SendAsync(userReq)).Content.ReadAsStringAsync()).RootElement.GetProperty("data");
                var id = xUser.GetProperty("id").GetString();
                var name = xUser.TryGetProperty("name", out var n) ? n.GetString() : "X";
                return new ProviderIdentity(id!, null, false, name ?? "X", "User")
                {
                    LinkBlockedReason = null
                };
            }
            default:
                return null;
        }
    }

    private async Task<LoginResponse?> ResolveUserAsync(string provider, ProviderIdentity identity)
    {
        var existingLink = await _context.ExternalLogins
            .Include(l => l.User).ThenInclude(u => u.Role)
            .Include(l => l.User).ThenInclude(u => u.Department)
            .FirstOrDefaultAsync(l => l.Provider == provider.ToLowerInvariant() && l.ProviderKey == identity.Subject);

        if (existingLink != null)
        {
            return BuildLogin(existingLink.User);
        }

        if (!string.IsNullOrWhiteSpace(identity.Email))
        {
            var existingUser = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Email == identity.Email);

            if (existingUser != null)
            {
                if (!identity.EmailVerified)
                {
                    identity.LinkBlockedReason =
                        "An account already exists with this email. Sign in with email and password. Unverified provider emails are not linked automatically.";
                    return null;
                }

                identity.LinkBlockedReason =
                    "An account already exists with this email. Sign in with your password first; automatic linking is disabled unless an administrator links the provider.";
                return null;
            }
        }

        var department = await _context.Departments.OrderBy(d => d.Id).FirstOrDefaultAsync();
        if (department == null)
        {
            identity.LinkBlockedReason = "No department is available to attach a new social account.";
            return null;
        }

        var email = identity.Email ?? $"{provider}-{identity.Subject}@oauth.local";
        var employeeId = $"EXT-{provider[..Math.Min(2, provider.Length)].ToUpperInvariant()}{identity.Subject}".Length > 50
            ? $"EXT{identity.Subject}"[..50]
            : $"EXT-{provider.ToUpperInvariant()}-{identity.Subject}"[..Math.Min(50, $"EXT-{provider.ToUpperInvariant()}-{identity.Subject}".Length)];

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))),
            FirstName = identity.FirstName,
            LastName = identity.LastName,
            EmployeeId = employeeId,
            DepartmentId = department.Id,
            RoleId = 1,
            IsActive = true,
            HireDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.ExternalLogins.Add(new ExternalLogin
        {
            UserId = user.Id,
            Provider = provider.ToLowerInvariant(),
            ProviderKey = identity.Subject,
            Email = identity.Email,
            EmailVerified = identity.EmailVerified,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var loaded = await _context.Users.Include(u => u.Role).Include(u => u.Department).FirstAsync(u => u.Id == user.Id);
        return BuildLogin(loaded);
    }

    private LoginResponse BuildLogin(User user)
    {
        return new LoginResponse
        {
            Token = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.Name),
            RefreshToken = _tokenService.GenerateRefreshToken(user.Id),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmployeeId = user.EmployeeId,
                Role = user.Role.Name,
                Department = user.Department.Name
            }
        };
    }

    private IConfigurationSection? ProviderSection(string provider)
    {
        var key = provider.Equals("x", StringComparison.OrdinalIgnoreCase) ? "X" : char.ToUpper(provider[0]) + provider[1..].ToLower();
        if (provider.Equals("github", StringComparison.OrdinalIgnoreCase)) key = "GitHub";
        if (provider.Equals("google", StringComparison.OrdinalIgnoreCase)) key = "Google";
        return _configuration.GetSection($"Authentication:{key}");
    }

    private static async Task<JsonElement> PostForm(HttpClient client, string url, Dictionary<string, string> form)
    {
        var response = await client.PostAsync(url, new FormUrlEncodedContent(form));
        var body = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(body).RootElement;
    }

    private static void Cleanup()
    {
        foreach (var kv in Pending)
        {
            if (kv.Value.ExpiresAt < DateTime.UtcNow)
            {
                Pending.TryRemove(kv.Key, out _);
            }
        }
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record PendingAuth(string Provider, string Verifier, string Nonce, DateTime ExpiresAt);

    private sealed class ProviderIdentity
    {
        public ProviderIdentity(string subject, string? email, bool emailVerified, string firstName, string lastName)
        {
            Subject = subject;
            Email = email;
            EmailVerified = emailVerified;
            FirstName = firstName;
            LastName = lastName;
        }

        public string Subject { get; }
        public string? Email { get; }
        public bool EmailVerified { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string? LinkBlockedReason { get; set; }
    }
}
