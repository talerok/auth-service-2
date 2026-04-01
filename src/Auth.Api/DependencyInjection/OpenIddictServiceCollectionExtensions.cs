using System.Security.Cryptography.X509Certificates;
using Auth.Api.Handlers;
using Auth.Application;
using Auth.Infrastructure;

namespace Auth.Api;

public static class OpenIddictServiceCollectionExtensions
{
    public static IServiceCollection AddOpenIddictServer(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var integration = configuration.GetSection("Integration").Get<IntegrationOptions>()
            ?? new IntegrationOptions();
        var oidc = integration.Oidc;
        var isDev = environment.IsDevelopment() || environment.IsEnvironment("Testing");

        services.AddOpenIddict()
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/connect/token")
                      .SetUserInfoEndpointUris("/connect/userinfo")
                      .SetAuthorizationEndpointUris("/connect/authorize")
                      .SetEndSessionEndpointUris("/connect/logout")
                      .SetRevocationEndpointUris("/connect/revocation")
                      .SetIntrospectionEndpointUris("/connect/introspect");

                options.RequireProofKeyForCodeExchange();

                options.AllowRefreshTokenFlow()
                      .AllowClientCredentialsFlow()
                      .AllowAuthorizationCodeFlow()
                      .AllowPasswordFlow()
                      .AllowCustomFlow(OidcConstants.MfaOtpGrantType)
                      .AllowCustomFlow(OidcConstants.JwtBearerGrantType)
                      .AllowCustomFlow(OidcConstants.LdapGrantType);

                options.RegisterScopes("openid", "profile", "email", "phone", "offline_access", "ws:*");

                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(oidc.AccessTokenLifetimeMinutes));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(oidc.RefreshTokenLifetimeDays));
                options.SetRefreshTokenReuseLeeway(TimeSpan.FromSeconds(oidc.RefreshTokenReuseLeewaySeconds));

                options.DisableAccessTokenEncryption();

                ConfigureSigningCertificate(options, oidc, isDev);
                ConfigureEncryptionCertificate(options, oidc, isDev);

                var aspNetCoreBuilder = options.UseAspNetCore()
                      .EnableTokenEndpointPassthrough()
                      .EnableUserInfoEndpointPassthrough()
                      .EnableAuthorizationEndpointPassthrough()
                      .EnableEndSessionEndpointPassthrough()
                      .EnableStatusCodePagesIntegration();

                if (isDev)
                    aspNetCoreBuilder.DisableTransportSecurityRequirement();

                options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.HandleIntrospectionRequestContext>(b =>
                    b.UseScopedHandler<ValidateSessionOnIntrospection>());
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }

    private static void ConfigureSigningCertificate(
        OpenIddictServerBuilder options, OidcOptions oidc, bool isDev)
    {
        if (!string.IsNullOrWhiteSpace(oidc.SigningKeyPath))
        {
            options.AddSigningCertificate(new X509Certificate2(oidc.SigningKeyPath, oidc.SigningKeyPassword));
            return;
        }

        if (!isDev)
            throw new InvalidOperationException("OIDC signing key is required in non-development environments.");

        var certPath = Path.Combine(oidc.DevCertDirectory, "dev-signing.pfx");
        if (!File.Exists(certPath))
            throw new InvalidOperationException(
                $"Dev signing certificate not found at '{certPath}'. Run: python3 generate-dev-certs.py");

        options.AddSigningCertificate(new X509Certificate2(certPath));
    }

    private static void ConfigureEncryptionCertificate(
        OpenIddictServerBuilder options, OidcOptions oidc, bool isDev)
    {
        if (!string.IsNullOrWhiteSpace(oidc.EncryptionKeyPath))
        {
            options.AddEncryptionCertificate(new X509Certificate2(oidc.EncryptionKeyPath, oidc.EncryptionKeyPassword));
            return;
        }

        if (!isDev)
            throw new InvalidOperationException("OIDC encryption key is required in non-development environments.");

        var certPath = Path.Combine(oidc.DevCertDirectory, "dev-encryption.pfx");
        if (!File.Exists(certPath))
            throw new InvalidOperationException(
                $"Dev encryption certificate not found at '{certPath}'. Run: python3 generate-dev-certs.py");

        options.AddEncryptionCertificate(new X509Certificate2(certPath));
    }
}
