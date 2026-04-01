namespace Auth.Infrastructure.DistributedJobs;

internal interface IDistributedJob
{
    string LockResource { get; }
    TimeSpan Interval { get; }
    int MaxBatchIterations { get; }
    Task<int> ExecuteAsync(CancellationToken ct);
}
