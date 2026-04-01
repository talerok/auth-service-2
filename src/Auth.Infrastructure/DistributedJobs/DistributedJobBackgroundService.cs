using Auth.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.DistributedJobs;

internal sealed class DistributedJobBackgroundService<TJob>(
    IServiceProvider serviceProvider,
    IDistributedLock distributedLock,
    ILogger<DistributedJobBackgroundService<TJob>> logger) : BackgroundService
    where TJob : IDistributedJob
{
    private static readonly string JobName = typeof(TJob).Name;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();
            var job = scope.ServiceProvider.GetRequiredService<TJob>();

            if (job.Interval <= TimeSpan.Zero)
                return;

            await RunWithLockAsync(job, stoppingToken);
            await Task.Delay(job.Interval, stoppingToken);
        }
    }

    private async Task RunWithLockAsync(TJob job, CancellationToken ct)
    {
        try
        {
            await using var handle = await distributedLock.TryAcquireAsync(job.LockResource, ct);
            if (handle is null)
                return;

            var totalProcessed = await ExecuteBatchLoopAsync(job, ct);

            if (totalProcessed > 0)
                logger.LogInformation("Job {Job} processed {Count} records", JobName, totalProcessed);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {Job} failed", JobName);
        }
    }

    private static async Task<int> ExecuteBatchLoopAsync(TJob job, CancellationToken ct)
    {
        var total = 0;
        var iterations = 0;
        while (!ct.IsCancellationRequested && iterations < job.MaxBatchIterations)
        {
            var processed = await job.ExecuteAsync(ct);
            total += processed;
            iterations++;
            if (processed == 0) break;
        }
        return total;
    }
}
