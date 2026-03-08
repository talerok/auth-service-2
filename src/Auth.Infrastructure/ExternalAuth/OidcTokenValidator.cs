using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using Auth.Application;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure;

internal interface IOidcTokenValidator
{
    Task<string> ValidateAndGetSubjectAsync(string authority, string clientId, string token, CancellationToken cancellationToken);
}

internal sealed class OidcTokenValidator : IOidcTokenValidator
{
    private static readonly JwtSecurityTokenHandler TokenHandler = new();

    private readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _configManagers = new();

    public async Task<string> ValidateAndGetSubjectAsync(
        string authority, string clientId, string token, CancellationToken cancellationToken)
    {
        var configManager = _configManagers.GetOrAdd(authority, key =>
            new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{key.TrimEnd('/')}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever()));

        OpenIdConnectConfiguration openIdConfig;
        try
        {
            openIdConfig = await configManager.GetConfigurationAsync(cancellationToken);
        }
        catch
        {
            throw new AuthException(AuthErrorCatalog.IdentitySourceTokenInvalid);
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = clientId,
            ValidateLifetime = true,
            IssuerSigningKeys = openIdConfig.SigningKeys,
            ValidateIssuerSigningKey = true
        };

        try
        {
            var principal = TokenHandler.ValidateToken(token, validationParameters, out _);
            var sub = principal.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(sub))
                throw new AuthException(AuthErrorCatalog.IdentitySourceTokenInvalid);

            return sub;
        }
        catch (AuthException)
        {
            throw;
        }
        catch
        {
            throw new AuthException(AuthErrorCatalog.IdentitySourceTokenInvalid);
        }
    }

}
