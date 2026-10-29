using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WelfareSystem.Api.Services;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/auth/external")]
public class ExternalAuthController : ControllerBase
{
    private readonly ExternalAuthService _externalAuth;
    private readonly IConfiguration _configuration;

    public ExternalAuthController(ExternalAuthService externalAuth, IConfiguration configuration)
    {
        _externalAuth = externalAuth;
        _configuration = configuration;
    }

    [HttpGet("providers")]
    [AllowAnonymous]
    public IActionResult Providers()
    {
        return Ok(_externalAuth.GetProviderStatus());
    }

    [HttpGet("{provider}/start")]
    [AllowAnonymous]
    public IActionResult Start(string provider)
    {
        if (!_externalAuth.IsConfigured(provider))
        {
            var frontend = FrontendBase();
            return Redirect($"{frontend}/login?error={Uri.EscapeDataString($"{provider} is not configured. Email sign-in is still available.")}");
        }

        var callback = $"{Request.Scheme}://{Request.Host}/api/auth/external/{provider}/callback";
        var url = _externalAuth.BuildAuthorizationUrl(provider, callback);
        if (url == null)
        {
            return BadRequest(new { message = "Unsupported provider." });
        }

        return Redirect(url);
    }

    [HttpGet("{provider}/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        var frontend = FrontendBase();
        if (!string.IsNullOrEmpty(error))
        {
            return Redirect($"{frontend}/auth/callback?error={Uri.EscapeDataString(error)}");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return Redirect($"{frontend}/auth/callback?error={Uri.EscapeDataString("The provider cancelled sign-in or returned an invalid callback.")}");
        }

        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/auth/external/{provider}/callback";
        var (login, fail) = await _externalAuth.CompleteAsync(provider, code, state, callbackUrl);
        if (login == null)
        {
            return Redirect($"{frontend}/auth/callback?error={Uri.EscapeDataString(fail ?? "Social sign-in failed.")}");
        }

        var exchange = _externalAuth.StoreExchange(login);
        return Redirect($"{frontend}/auth/callback?code={Uri.EscapeDataString(exchange)}");
    }

    [HttpPost("exchange")]
    [AllowAnonymous]
    public IActionResult Exchange([FromBody] ExchangeRequest request)
    {
        var login = _externalAuth.ConsumeExchange(request.Code);
        if (login == null)
        {
            return Unauthorized(new { message = "Invalid or expired social sign-in code." });
        }

        return Ok(login);
    }

    private string FrontendBase() =>
        _configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:3000";

    public class ExchangeRequest
    {
        public string Code { get; set; } = string.Empty;
    }
}
