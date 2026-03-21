using System.Security.Cryptography.X509Certificates;
using Auth.Api;
using Auth.Application;
using Auth.Api.HealthChecks;
using Auth.Infrastructure;
using Auth.Infrastructure.Integration;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgresql")
    .AddCheck<OpenSearchHealthCheck>("opensearch");

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddInfrastructureIntegration(builder.Configuration);

var integration = builder.Configuration.GetSection("Integration").Get<IntegrationOptions>() ?? new IntegrationOptions();
var oidc = integration.Oidc;

builder.Services.AddOpenIddict()
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token")
              .SetUserInfoEndpointUris("/connect/userinfo")
              .SetAuthorizationEndpointUris("/connect/authorize")
              .SetEndSessionEndpointUris("/connect/logout")
              .SetRevocationEndpointUris("/connect/revocation");

        options.AllowRefreshTokenFlow()
              .AllowClientCredentialsFlow()
              .AllowAuthorizationCodeFlow()
              .AllowPasswordFlow()
              .AllowCustomFlow(OidcConstants.MfaOtpGrantType)
              .AllowCustomFlow(OidcConstants.TokenExchangeGrantType);

        options.RegisterScopes("openid", "profile", "email", "phone", "ws", "offline_access");

        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(oidc.AccessTokenLifetimeMinutes));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(oidc.RefreshTokenLifetimeDays));

        options.DisableAccessTokenEncryption();

        var isDev = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");

        if (!string.IsNullOrWhiteSpace(oidc.SigningKeyPath))
            options.AddSigningCertificate(new X509Certificate2(oidc.SigningKeyPath, oidc.SigningKeyPassword));
        else if (isDev)
            options.AddDevelopmentSigningCertificate();
        else
            throw new InvalidOperationException("OIDC signing key is required in non-development environments.");

        if (!string.IsNullOrWhiteSpace(oidc.EncryptionKeyPath))
            options.AddEncryptionCertificate(new X509Certificate2(oidc.EncryptionKeyPath, oidc.EncryptionKeyPassword));
        else if (isDev)
            options.AddDevelopmentEncryptionCertificate();
        else
            throw new InvalidOperationException("OIDC encryption key is required in non-development environments.");

        var aspNetCoreBuilder = options.UseAspNetCore()
              .EnableTokenEndpointPassthrough()
              .EnableUserInfoEndpointPassthrough()
              .EnableAuthorizationEndpointPassthrough()
              .EnableEndSessionEndpointPassthrough()
              .EnableStatusCodePagesIntegration();

        if (isDev)
            aspNetCoreBuilder.DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

if (integration.Cors.AllowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy
                .WithOrigins(integration.Cors.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
})
.AddCookie("Identity.External", options =>
{
    options.LoginPath = new PathString(oidc.LoginUrl);
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionInHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment()
    && !app.Environment.IsEnvironment("Testing")
    && !string.IsNullOrWhiteSpace(integration.TwoFactor.StaticOtpForTesting))
{
    throw new InvalidOperationException(
        "StaticOtpForTesting must not be set outside the Development/Testing environment.");
}

await app.Services.SeedAsync(CancellationToken.None);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ProblemDetailsMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.Run();
