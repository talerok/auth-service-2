using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenSearch.Client;

namespace Auth.Api.HealthChecks;

internal sealed class OpenSearchHealthCheck(IOpenSearchClient openSearchClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await openSearchClient.PingAsync(x => x, cancellationToken);
            return response.IsValid
                ? HealthCheckResult.Healthy("OpenSearch is reachable.")
                : HealthCheckResult.Unhealthy($"OpenSearch ping failed: {response.DebugInformation}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("OpenSearch health check threw an exception.", ex);
        }
    }
}
