using Auth.Application;
using Auth.Infrastructure.Cors;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Auth.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntegrationOptions>(configuration.GetSection("Integration"));
        services.Configure<PasswordRequirementsOptions>(configuration.GetSection("Integration:PasswordRequirements"));
        var integration = configuration.GetSection("Integration").Get<IntegrationOptions>() ?? new IntegrationOptions();

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(integration.PostgreSql.ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();
        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(dataSource));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));
        services.AddValidatorsFromAssembly(typeof(AuthException).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLogs.AuditBehavior<,>));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IOidcTokenValidator, OidcTokenValidator>();
        services.AddScoped<ILdapAuthenticator, LdapAuthenticator>();
        services.AddSingleton<IPermissionBitCache, PermissionBitCache>();
        services.AddMemoryCache();
        services.AddSingleton<ICorsOriginService, CorsOriginService>();
        services.AddHttpContextAccessor();
        services.AddScoped<IAuditService, AuditLogs.AuditService>();
        services.AddScoped<IAuditContext, AuditLogs.AuditContext>();
        services.AddScoped<ISearchIndexService, NullSearchIndexService>();

        if (integration.Smtp.Enabled)
            services.AddSingleton<ITwoFactorEmailGateway, SmtpTwoFactorEmailGateway>();
        else
            services.AddSingleton<ITwoFactorEmailGateway, SafeDefaultTwoFactorEmailGateway>();

        if (integration.SmsGateway.Enabled)
        {
            services.AddHttpClient("SmsGateway", client =>
            {
                client.BaseAddress = new Uri(integration.SmsGateway.BaseUrl);
                client.DefaultRequestHeaders.Add("X-Api-Key", integration.SmsGateway.ApiKey);
                client.Timeout = TimeSpan.FromSeconds(integration.SmsGateway.TimeoutSeconds);
            });
            services.AddSingleton<ITwoFactorSmsGateway, HttpTwoFactorSmsGateway>();
        }
        else
        {
            services.AddSingleton<ITwoFactorSmsGateway, SafeDefaultTwoFactorSmsGateway>();
        }

        services.AddHostedService<TwoFactorDeliveryBackgroundService>();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                      .UseDbContext<AuthDbContext>();
            });

        return services;
    }
}
