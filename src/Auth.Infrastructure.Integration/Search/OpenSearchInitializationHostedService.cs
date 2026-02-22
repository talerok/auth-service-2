using Auth.Application;
using Auth.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Integration.Search;

public sealed class OpenSearchInitializationHostedService(
    IServiceProvider serviceProvider,
    IOptions<IntegrationOptions> options,
    ILogger<OpenSearchInitializationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var openSearchOptions = options.Value.OpenSearch;
        if (!openSearchOptions.EnsureIndicesOnStartup)
        {
            return;
        }

        try
        {
            using var scope = serviceProvider.CreateScope();
            var maintenanceService = scope.ServiceProvider.GetRequiredService<ISearchMaintenanceService>();
            await maintenanceService.EnsureIndicesAsync(cancellationToken);

            if (openSearchOptions.ReindexOnStartup)
            {
                await maintenanceService.ReindexAllAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OpenSearch startup initialization failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
