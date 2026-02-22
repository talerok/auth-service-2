using Auth.Application;
using Auth.Infrastructure;
using Auth.Infrastructure.Integration.Kafka;
using Auth.Infrastructure.Integration.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSearch.Client;

namespace Auth.Infrastructure.Integration;

public static class OpenSearchServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var integration = configuration.GetSection("Integration").Get<IntegrationOptions>() ?? new IntegrationOptions();
        services.AddSingleton<IOpenSearchClient>(_ =>
        {
            var uri = new Uri(integration.OpenSearch.Url);
            var settings = new ConnectionSettings(uri).DefaultIndex($"{integration.OpenSearch.IndexPrefix}-users");
            if (!string.IsNullOrWhiteSpace(integration.OpenSearch.Username))
            {
                settings = settings.BasicAuthentication(integration.OpenSearch.Username, integration.OpenSearch.Password);
            }

            return new OpenSearchClient(settings);
        });

        services.AddSingleton<OpenSearchIndexNames>();
        services.AddScoped<OpenSearchRetryExecutor>();
        services.AddScoped<ISearchService, OpenSearchQueryService>();
        services.AddScoped<ISearchIndexService, OpenSearchIndexService>();
        services.AddScoped<ISearchMaintenanceService, OpenSearchMaintenanceService>();
        services.AddHostedService<OpenSearchInitializationHostedService>();

        if (integration.Kafka.Enabled)
        {
            services.AddSingleton<IKafkaProducer, ConfluentKafkaProducer>();
        }
        else
        {
            services.AddSingleton<IKafkaProducer, NullKafkaProducer>();
        }

        return services;
    }
}
