using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.DistributedJobs.Cleanup;

internal sealed class AuditLogCleanupJob(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options) : IDistributedJob
{
    private readonly AuditLogCleanupOptions _options = Validate(options.Value.Cleanup.AuditLog);

    public string LockResource => "cleanup:audit-log";
    public TimeSpan Interval => TimeSpan.FromMinutes(_options.IntervalMinutes);
    public int MaxBatchIterations => 100;

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_options.RetentionDays);

        return await dbContext.AuditLogEntries
            .Where(e => e.Timestamp < cutoff)
            .OrderBy(e => e.Timestamp)
            .Take(_options.BatchSize)
            .ExecuteDeleteAsync(ct);
    }

    private static AuditLogCleanupOptions Validate(AuditLogCleanupOptions o)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(o.RetentionDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(o.BatchSize);
        return o;
    }
}
