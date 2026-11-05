using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using WelfareSystem.Api.Controllers;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Services;
using WelfareSystem.Api.Services.Interfaces;

public static class SocialAuthChecks
{
    public static async Task Run(AppDbContext db, int ownerId, string ownerPassword, int otherId, string otherPassword,
        ITokenService tokens, Action<bool, string> check)
    {
        var clock = new TestClock();
        var store = new ExternalAuthFlowStore(clock);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Frontend:BaseUrl"] = "http://localhost:3000", ["Authentication:BackendBaseUrl"] = "http://localhost:5249",
            ["Authentication:Google:ClientId"] = "test-google", ["Authentication:Google:ClientSecret"] = "test-secret",
            ["Authentication:GitHub:ClientId"] = "test-github", ["Authentication:GitHub:ClientSecret"] = "test-secret",
            ["Authentication:X:ClientId"] = "test-x", ["Authentication:X:ClientSecret"] = "test-secret"
        }).Build();
        var settings = new ExternalAuthSettings(config, new TestEnvironment());
        var http = new TestHttpFactory();
        var google = new TestGoogleValidator();
        var auth = new ExternalAuthService(db, new PasswordHasher(), tokens, http, store, google, settings);
        check(auth.GetProviderStatus().All(p => p.Configured) && !auth.IsConfigured("unknown"), "Only supported configured social providers are enabled");
        check(!settings.AllowedOrigin("https://attacker.invalid") && settings.AllowedOrigin("http://localhost:3000"), "Only the configured frontend origin is trusted");
        config["Authentication:Google:CallbackUrl"] = "https://attacker.invalid/callback";
        check(!auth.IsConfigured("google"), "A mismatched configured callback disables the provider");
        config["Authentication:Google:CallbackUrl"] = null;
        var production = new ExternalAuthSettings(config, new TestEnvironment { EnvironmentName = Environments.Production });
        check(!production.IsConfigured("google"), "Production rejects plain HTTP callback origins");
        var empty = new ExternalAuthSettings(new ConfigurationBuilder().Build(), new TestEnvironment());
        check(!empty.IsConfigured("google"), "Missing provider credentials never enable sign-in");

        foreach (var provider in new[] { "google", "github", "x" }) {
            var start = await auth.StartAsync(provider, "browser-binding");
            var query = QueryHelpers.ParseQuery(new Uri(start.AuthorizationUrl).Query);
            check(query["code_challenge_method"] == "S256" && query.ContainsKey("code_challenge") && query.ContainsKey("state"), $"{provider} authorization includes state and PKCE");
            check(query["redirect_uri"] == settings.Callback(provider), $"{provider} redirect uses the fixed server callback");
        }
        var beforeCalls = http.Calls.Count;
        check((await auth.CompleteAsync("github", "code", "invalid-state", "browser")).Error != null && http.Calls.Count == beforeCalls,
            "Invalid OAuth state is rejected before contacting the provider");
        var bound = await auth.StartAsync("github", "right-browser");
        check((await auth.CompleteAsync("github", "code", bound.State, "wrong-browser")).Error != null, "OAuth callback is bound to the originating browser");
        check((await auth.CompleteAsync("x", "code", bound.State, "right-browser")).Error != null, "OAuth state cannot be used with a different provider");
        var cancelled = await auth.CompleteAsync("github", null, bound.State, "right-browser", "access_denied");
        check(cancelled.Error?.Contains("cancelled") == true, "Cancelled provider authorization has a clear failure result");
        check((await auth.CompleteAsync("github", "code", bound.State, "right-browser")).Error != null, "Cancelled state cannot be replayed");
        var expired = await auth.StartAsync("github", "browser");
        clock.Advance(TimeSpan.FromMinutes(11));
        check((await auth.CompleteAsync("github", "code", expired.State, "browser")).Error != null, "OAuth state expires after ten minutes");
        var rejectedPassword = false;
        try { await auth.StartAsync("google", "browser", ownerId, "incorrect"); } catch (UnauthorizedAccessException) { rejectedPassword = true; }
        check(rejectedPassword, "Provider linking requires the current account password");

        var userCount = await db.Users.CountAsync();
        var unknown = await auth.StartAsync("google", "browser");
        var noAutoLink = await auth.CompleteAsync("google", "code", unknown.State, "browser");
        check(noAutoLink.Error?.Contains("not linked") == true && await db.Users.CountAsync() == userCount,
            "Unknown provider identities do not auto-create employees or link by email");

        foreach (var provider in new[] { "google", "github", "x" }) {
            var linkStart = await auth.StartAsync(provider, "link-browser", ownerId, ownerPassword);
            var linked = await auth.CompleteAsync(provider, "code", linkStart.State, "link-browser");
            check(linked.Linked && linked.Login == null && linked.Error == null, $"{provider} identity can be explicitly linked after password confirmation");
            var signIn = await auth.StartAsync(provider, "signin-browser");
            var logged = await auth.CompleteAsync(provider, "code", signIn.State, "signin-browser");
            var owner = await db.Users.Include(u => u.Role).SingleAsync(u => u.Id == ownerId);
            check(logged.Login?.User.Id == ownerId && logged.Login.User.Role == owner.Role.Name,
                $"{provider} linked sign-in retains the server-assigned account and role");
            check((await auth.CompleteAsync(provider, "code", signIn.State, "signin-browser")).Error != null, $"{provider} successful callback state cannot be replayed");
            if (provider == "google") {
                var exchange = auth.StoreExchange(logged.Login!, "signin-browser");
                check(await auth.ConsumeExchangeAsync(exchange, "different-browser") == null, "Frontend sign-in exchange is browser-bound");
                check(await auth.ConsumeExchangeAsync(exchange, "signin-browser") != null, "A matching browser can consume the exchange code");
                check(await auth.ConsumeExchangeAsync(exchange, "signin-browser") == null, "Frontend exchange codes are single-use");
                var old = auth.StoreExchange(logged.Login!, "signin-browser");
                clock.Advance(TimeSpan.FromSeconds(61));
                check(await auth.ConsumeExchangeAsync(old, "signin-browser") == null, "Frontend exchange codes expire after sixty seconds");
            }
        }
        check(http.Forms.All(f => f.Contains("code_verifier=")), "Provider token exchanges include the original PKCE verifier");
        var collision = await auth.StartAsync("google", "other-browser", otherId, otherPassword);
        check((await auth.CompleteAsync("google", "code", collision.State, "other-browser")).Error != null,
            "A provider identity linked to another account cannot be taken over");
        var changedAccount = await auth.StartAsync("github", "changed-browser", ownerId, ownerPassword);
        var dbOwner = await db.Users.SingleAsync(u => u.Id == ownerId);
        dbOwner.TokenVersion++; await db.SaveChangesAsync();
        check((await auth.CompleteAsync("github", "code", changedAccount.State, "changed-browser")).Error != null,
            "Account changes invalidate an in-progress provider link");
        var suspended = await auth.StartAsync("google", "suspended-browser");
        dbOwner.IsActive = false; await db.SaveChangesAsync();
        check((await auth.CompleteAsync("google", "code", suspended.State, "suspended-browser")).Login == null,
            "Suspended accounts cannot use linked social sign-in");
        dbOwner.IsActive = true; await db.SaveChangesAsync();
        http.Fail = true;
        var failing = await auth.StartAsync("github", "browser");
        var failure = await auth.CompleteAsync("github", "code", failing.State, "browser");
        check(failure.Error != null && !failure.Error.Contains("sensitive-provider-detail"), "Provider failures are handled without exposing provider internals");
        http.Fail = false;
        check(await db.AuditLogs.AnyAsync(a => a.UserId == ownerId && a.Action == "SocialProviderLinked"), "Provider linking records an audit event");

        var controller = new ExternalAuthController(auth, settings, db) { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        controller.Request.Headers.Origin = "https://attacker.invalid";
        check(await controller.Exchange(new() { Code = "unused" }) is ObjectResult { StatusCode: 403 }, "The exchange endpoint rejects an untrusted Origin header");
        controller.Request.Headers.Origin = "http://localhost:3000";
        check(await controller.Exchange(new() { Code = "unused" }) is UnauthorizedObjectResult, "The exchange endpoint rejects missing browser cookies");
        check(await controller.Start("github") is RedirectResult && controller.Response.Headers.SetCookie.ToString().Contains("httponly", StringComparison.OrdinalIgnoreCase),
            "OAuth initiation creates an HttpOnly correlation cookie");

        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test-signing-key" };
        string Sign(string audience = "test-google", string nonce = "expected-nonce", string issuer = "https://accounts.google.com", bool expiredToken = false, string? azp = null)
        {
            var claims = new List<Claim> { new("sub", "signed-google-subject"), new("nonce", nonce) };
            if (azp != null) claims.Add(new("azp", azp));
            var jwt = new JwtSecurityToken(issuer, audience, claims, DateTime.UtcNow.AddMinutes(-10),
                expiredToken ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5), new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
        bool Reject(string token, IEnumerable<SecurityKey>? keys = null) {
            try { GoogleIdTokenValidator.ValidateWithKeys(token, "test-google", "expected-nonce", keys ?? [key]); return false; }
            catch (SecurityTokenException) { return true; }
        }
        check(GoogleIdTokenValidator.ValidateWithKeys(Sign(), "test-google", "expected-nonce", [key]).FindFirst("sub")?.Value == "signed-google-subject",
            "Google ID-token validation accepts a signed token with correct claims");
        check(Reject(Sign(audience: "another-client")), "Google ID tokens with the wrong audience are rejected");
        check(Reject(Sign(nonce: "wrong-nonce")), "Google ID tokens with the wrong nonce are rejected");
        check(Reject(Sign(issuer: "https://attacker.invalid")), "Google ID tokens with an untrusted issuer are rejected");
        check(Reject(Sign(expiredToken: true)), "Expired Google ID tokens are rejected");
        check(Reject(Sign(azp: "another-client")), "Google ID tokens with the wrong authorized party are rejected");
        using var otherRsa = RSA.Create(2048);
        check(Reject(Sign(), [new RsaSecurityKey(otherRsa) { KeyId = "different-key" }]), "Google ID tokens signed by an unknown key are rejected");
    }

    private sealed class TestClock : TimeProvider {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan value) => _now += value;
    }
    private sealed class TestEnvironment : IHostEnvironment {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Regression";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    private sealed class TestGoogleValidator : IGoogleIdTokenValidator {
        public Task<ClaimsPrincipal> ValidateAsync(string token, string audience, string nonce, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "test-linked-google-subject"), new Claim("email_verified", "false") })));
    }
    private sealed class TestHttpFactory : IHttpClientFactory {
        public bool Fail { get; set; }
        public List<string> Calls { get; } = new();
        public List<string> Forms { get; } = new();
        public HttpClient CreateClient(string name) => new(new Handler(this));
        private sealed class Handler(TestHttpFactory owner) : HttpMessageHandler {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
                var url = request.RequestUri!.AbsoluteUri; owner.Calls.Add(url);
                if (request.Method == HttpMethod.Post) owner.Forms.Add(await request.Content!.ReadAsStringAsync(ct));
                if (owner.Fail) return new(HttpStatusCode.BadGateway) { Content = new StringContent("sensitive-provider-detail") };
                var body = url switch {
                    "https://oauth2.googleapis.com/token" => "{\"id_token\":\"simulated-provider-id-token\"}",
                    "https://github.com/login/oauth/access_token" or "https://api.x.com/2/oauth2/token" => "{\"access_token\":\"test-only-access-token\",\"token_type\":\"bearer\"}",
                    "https://api.github.com/user" => "{\"id\":89401234}",
                    "https://api.github.com/user/emails" => "[{\"email\":\"unverified@example.invalid\",\"primary\":true,\"verified\":false}]",
                    "https://api.x.com/2/users/me" => "{\"data\":{\"id\":\"89405678\"}}",
                    _ => throw new InvalidOperationException("Unexpected test endpoint; external network access is forbidden in this fixture.")
                };
                return new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
            }
        }
    }
}
