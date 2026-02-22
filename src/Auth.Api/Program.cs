using System.Text;
using Auth.Api;
using Auth.Application;
using Auth.Api.HealthChecks;
using Auth.Infrastructure;
using Auth.Infrastructure.Integration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
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
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = integration.Jwt.Issuer,
            ValidAudience = integration.Jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(integration.Jwt.Secret))
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                if (context.Response.HasStarted)
                {
                    return Task.CompletedTask;
                }

                context.HandleResponse();
                var code = string.IsNullOrWhiteSpace(context.Error)
                    ? AuthErrorCatalog.AuthenticationRequired
                    : AuthErrorCatalog.AuthenticationFailed;
                var problem = AuthProblemDetailsMapper.Map(code);
                return ProblemDetailsResponseWriter.WriteAsync(context.HttpContext, problem, code);
            },
            OnForbidden = context =>
            {
                if (context.Response.HasStarted)
                {
                    return Task.CompletedTask;
                }

                var code = AuthErrorCatalog.AccessDenied;
                var problem = AuthProblemDetailsMapper.Map(code);
                return ProblemDetailsResponseWriter.WriteAsync(context.HttpContext, problem, code);
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

var app = builder.Build();

await app.Services.SeedAsync(CancellationToken.None);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ProblemDetailsMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.Run();
