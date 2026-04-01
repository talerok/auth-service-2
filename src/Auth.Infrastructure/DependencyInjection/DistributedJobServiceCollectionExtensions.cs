using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure.DistributedJobs;

internal static class DistributedJobServiceCollectionExtensions
{
    public static IServiceCollection AddDistributedJob<TJob>(this IServiceCollection services)
        where TJob : class, IDistributedJob
    {
        services.AddScoped<TJob>();
        services.AddHostedService<DistributedJobBackgroundService<TJob>>();
        return services;
    }
}
