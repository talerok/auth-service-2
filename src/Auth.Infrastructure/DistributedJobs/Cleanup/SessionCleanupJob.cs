using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.DistributedJobs.Cleanup;

internal sealed class SessionCleanupJob(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options) : IDistributedJob
{
    private readonly SessionCleanupOptions _options = Validate(options.Value.Cleanup.Sessions);

    public string LockResource => "cleanup:sessions";
    public TimeSpan Interval => TimeSpan.FromMinutes(_options.IntervalMinutes);
    public int MaxBatchIterations => 100;

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_options.RetentionDays);

        return await dbContext.UserSessions
            .Where(s => s.ExpiresAt < cutoff || (s.IsRevoked && s.RevokedAt < cutoff))
            .OrderBy(s => s.ExpiresAt)
            .Take(_options.BatchSize)
            .ExecuteDeleteAsync(ct);
    }

    private static SessionCleanupOptions Validate(SessionCleanupOptions o)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(o.RetentionDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(o.BatchSize);
        return o;
    }
}
