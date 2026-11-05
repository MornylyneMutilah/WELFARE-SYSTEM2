using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Services;

namespace WelfareSystem.Api.Controllers;

[ApiController, Route("api/auth/external"), EnableRateLimiting("external-auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ExternalAuthController(ExternalAuthService external, ExternalAuthSettings settings, AppDbContext context) : ControllerBase
{
    private const string CookiePath = "/api/auth/external";
    private const string ExchangeCookie = "welfare.oauth.exchange";
    private CookieOptions Cookie(TimeSpan lifetime) => new() {
        HttpOnly = true, Secure = settings.BackendBase.StartsWith("https://", StringComparison.Ordinal),
        SameSite = SameSiteMode.Lax, Path = CookiePath, MaxAge = lifetime, IsEssential = true
    };
    private static string CorrelationName(string state) => "welfare.oauth." + ExternalAuthFlowStore.Hash(state)[..24];
    private bool OriginAllowed() => settings.AllowedOrigin(Request.Headers.Origin.ToString());
    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private IActionResult Failure(string message) => Redirect(settings.FrontendBase + "/auth/callback?error=" + Uri.EscapeDataString(message));

    [HttpGet("providers"), AllowAnonymous]
    public IActionResult Providers() => Ok(external.GetProviderStatus());

    [HttpGet("links"), Authorize]
    public async Task<IActionResult> Links() => Ok(await context.ExternalLogins.AsNoTracking().Where(l => l.UserId == ActorId)
        .Select(l => new { l.Provider, l.CreatedAt }).ToListAsync());

    [HttpGet("{provider}/start"), AllowAnonymous]
    public async Task<IActionResult> Start(string provider)
    {
        if (!external.IsConfigured(provider)) return Failure("This provider is not configured. Email sign-in remains available.");
        var browserSecret = ExternalAuthFlowStore.RandomToken();
        try {
            var start = await external.StartAsync(provider, browserSecret);
            Response.Cookies.Append(CorrelationName(start.State), browserSecret, Cookie(TimeSpan.FromMinutes(10)));
            Response.Headers["Referrer-Policy"] = "no-referrer";
            return Redirect(start.AuthorizationUrl);
        } catch (InvalidOperationException) { return Failure("Provider sign-in could not be started. Please try again."); }
    }

    [HttpPost("{provider}/link"), Authorize]
    public async Task<IActionResult> Link(string provider, [FromBody] LinkRequest request)
    {
        if (!OriginAllowed()) return StatusCode(403, new { message = "Untrusted request origin." });
        if (!external.IsConfigured(provider)) return BadRequest(new { message = "This provider is not configured." });
        var browserSecret = ExternalAuthFlowStore.RandomToken();
        try {
            var start = await external.StartAsync(provider, browserSecret, ActorId, request.Password);
            Response.Cookies.Append(CorrelationName(start.State), browserSecret, Cookie(TimeSpan.FromMinutes(10)));
            return Ok(new { start.AuthorizationUrl });
        } catch (UnauthorizedAccessException) { return Unauthorized(new { message = "Password confirmation failed." }); }
        catch (InvalidOperationException) { return BadRequest(new { message = "Provider linking could not be started. Try again." }); }
    }

    [HttpGet("{provider}/callback"), AllowAnonymous]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (string.IsNullOrEmpty(state) || state.Length > 128) return Failure("Invalid sign-in callback. Start again.");
        var correlation = CorrelationName(state);
        var binding = Request.Cookies[correlation];
        Response.Cookies.Delete(correlation, Cookie(TimeSpan.Zero));
        var result = await external.CompleteAsync(provider, code, state, binding, error, HttpContext.RequestAborted);
        if (result.Error != null) return Failure(result.Error);
        if (result.Linked) return Redirect(settings.FrontendBase + "/employee/profile?providerLinked=" + Uri.EscapeDataString(provider));
        if (result.Login == null || string.IsNullOrEmpty(binding)) return Failure("The provider did not complete sign-in.");
        var exchange = external.StoreExchange(result.Login, binding);
        Response.Cookies.Append(ExchangeCookie, binding, Cookie(TimeSpan.FromSeconds(60)));
        return Redirect(settings.FrontendBase + "/auth/callback?code=" + Uri.EscapeDataString(exchange));
    }

    [HttpPost("exchange"), AllowAnonymous]
    public async Task<IActionResult> Exchange([FromBody] ExchangeRequest request)
    {
        if (!OriginAllowed()) return StatusCode(403, new { message = "Untrusted request origin." });
        var login = await external.ConsumeExchangeAsync(request.Code, Request.Cookies[ExchangeCookie]);
        Response.Cookies.Delete(ExchangeCookie, Cookie(TimeSpan.Zero));
        return login == null ? Unauthorized(new { message = "Invalid or expired social sign-in code. Start again." }) : Ok(login);
    }

    public class LinkRequest { [Required, MaxLength(1024)] public string Password { get; set; } = string.Empty; }
    public class ExchangeRequest { [Required, MaxLength(128)] public string Code { get; set; } = string.Empty; }
}
