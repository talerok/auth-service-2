using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Auth.Api.HealthChecks;

internal sealed class RedisHealthCheck(IDistributedCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.GetAsync("health", cancellationToken);
            return HealthCheckResult.Healthy("Redis is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis health check threw an exception.", ex);
        }
    }
}
