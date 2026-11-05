using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace WelfareSystem.Api.Services;

public interface IGoogleIdTokenValidator
{
    Task<ClaimsPrincipal> ValidateAsync(string token, string audience, string nonce, CancellationToken cancellationToken = default);
}
public sealed class GoogleIdTokenValidator : IGoogleIdTokenValidator
{
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _metadata;
    public GoogleIdTokenValidator(IHttpClientFactory factory)
    {
        _metadata = new("https://accounts.google.com/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(factory.CreateClient("ExternalAuth")) { RequireHttps = true });
    }
    public async Task<ClaimsPrincipal> ValidateAsync(string token, string audience, string nonce, CancellationToken cancellationToken = default)
    {
        var metadata = await _metadata.GetConfigurationAsync(cancellationToken);
        try { return ValidateWithKeys(token, audience, nonce, metadata.SigningKeys); }
        catch (SecurityTokenSignatureKeyNotFoundException) {
            _metadata.RequestRefresh();
            metadata = await _metadata.GetConfigurationAsync(cancellationToken);
            return ValidateWithKeys(token, audience, nonce, metadata.SigningKeys);
        }
    }
    public static ClaimsPrincipal ValidateWithKeys(string token, string audience, string nonce, IEnumerable<SecurityKey> keys)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token, new TokenValidationParameters {
            ValidateIssuerSigningKey = true, IssuerSigningKeys = keys, RequireSignedTokens = true,
            ValidateIssuer = true, ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
            ValidateAudience = true, ValidAudience = audience, ValidateLifetime = true, RequireExpirationTime = true,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256], ClockSkew = TimeSpan.FromSeconds(30)
        }, out var validated);
        var jwt = (JwtSecurityToken)validated;
        if (string.IsNullOrWhiteSpace(principal.FindFirstValue("sub")) ||
            !ExternalAuthFlowStore.Equal(principal.FindFirstValue("nonce") ?? "", nonce) ||
            (jwt.Audiences.Count() > 1 && principal.FindFirstValue("azp") != audience) ||
            (principal.FindFirstValue("azp") is string azp && azp != audience))
            throw new SecurityTokenValidationException("Invalid Google identity claims.");
        return principal;
    }
}
