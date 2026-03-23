using Auth.Application;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace Auth.Api.Cors;

internal sealed class ConfigureOidcCorsOptions(
    ICorsOriginService corsOriginService,
    IOptions<Auth.Infrastructure.IntegrationOptions> integration) : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions options)
    {
        var staticOrigins = integration.Value.Cors.GetParsedOrigins();

        options.AddPolicy("Oidc", policy =>
        {
            policy.SetIsOriginAllowed(origin => IsOriginAllowed(origin, staticOrigins))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    }

    private bool IsOriginAllowed(string origin, string[] staticOrigins) =>
        staticOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)
        || corsOriginService.IsOriginAllowed(origin);
}
