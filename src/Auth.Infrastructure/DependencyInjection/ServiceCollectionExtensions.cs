using Auth.Application;
using Auth.Infrastructure.Cors;
using Auth.Infrastructure.DistributedJobs;
using Auth.Infrastructure.DistributedJobs.Cleanup;
using Auth.Infrastructure.Locking;
using Auth.Infrastructure.Messaging;
using Auth.Infrastructure.Messaging.Consumers;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;

namespace Auth.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntegrationOptions>(configuration.GetSection("Integration"));
        services.Configure<PasswordRequirementsOptions>(configuration.GetSection("Integration:PasswordRequirements"));
        services.Configure<PasswordExpirationOptions>(configuration.GetSection("Integration:PasswordExpiration"));
        var integration = configuration.GetSection("Integration").Get<IntegrationOptions>() ?? new IntegrationOptions();

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(integration.PostgreSql.ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();
        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(dataSource));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));
        services.AddValidatorsFromAssembly(typeof(AuthException).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLogs.AuditBehavior<,>));

        services.AddSingleton<IDistributedLock, PgDistributedLock>();
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

        services.AddScoped<IEventBus, MassTransitEventBus>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<DeliverOtpConsumer, DeliverOtpConsumerDefinition>();
            x.AddConsumer<DeliverOtpFaultConsumer>();
            x.AddConsumer<IndexEntityConsumer, IndexEntityConsumerDefinition>();
            x.AddConsumer<IndexAuditLogConsumer, IndexAuditLogConsumerDefinition>();
            x.AddConsumer<PermissionCacheInvalidationConsumer>()
                .Endpoint(e => e.Temporary = true);
            x.AddConsumer<CorsOriginCacheInvalidationConsumer>()
                .Endpoint(e => e.Temporary = true);

            x.AddEntityFrameworkOutbox<AuthDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
                o.QueryDelay = TimeSpan.FromSeconds(1);
            });

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = integration.RabbitMq;
                cfg.Host(rabbit.Host, rabbit.Port, rabbit.VirtualHost, h =>
                {
                    h.Username(rabbit.Username);
                    h.Password(rabbit.Password);
                });
                cfg.ConfigureEndpoints(context);
            });
        });

        var redisConnectionString = integration.Redis.ConnectionString;
        var redis = ConnectionMultiplexer.Connect(redisConnectionString);
        services.AddSingleton<IConnectionMultiplexer>(redis);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "auth:";
        });

        services.AddDataProtection()
            .SetApplicationName("auth-service")
            .PersistKeysToStackExchangeRedis(redis, "auth:dataprotection-keys");

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                      .UseDbContext<AuthDbContext>();
            });

        services.AddDistributedJob<SessionCleanupJob>();
        services.AddDistributedJob<TwoFactorChallengeCleanupJob>();
        services.AddDistributedJob<PasswordChallengeCleanupJob>();
        services.AddDistributedJob<AuditLogCleanupJob>();

        return services;
    }
}
