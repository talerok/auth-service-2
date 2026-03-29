using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Auth.Api.HealthChecks;

internal sealed class RabbitMqHealthCheck(IBusControl busControl) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var health = busControl.CheckHealth();
            return Task.FromResult(health.Status == BusHealthStatus.Healthy
                ? HealthCheckResult.Healthy("RabbitMQ is reachable.")
                : HealthCheckResult.Unhealthy($"RabbitMQ health check failed: {health.Status}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ health check threw an exception.", ex));
        }
    }
}
