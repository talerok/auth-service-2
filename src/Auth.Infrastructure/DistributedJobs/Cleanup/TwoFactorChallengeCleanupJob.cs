using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.DistributedJobs.Cleanup;

internal sealed class TwoFactorChallengeCleanupJob(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options) : IDistributedJob
{
    private readonly TwoFactorCleanupOptions _options = Validate(options.Value.Cleanup.TwoFactorChallenges);

    public string LockResource => "cleanup:two-factor-challenges";
    public TimeSpan Interval => TimeSpan.FromMinutes(_options.IntervalMinutes);
    public int MaxBatchIterations => 100;

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddHours(-_options.RetentionHours);

        return await dbContext.TwoFactorChallenges
            .Where(c => c.ExpiresAt < cutoff)
            .OrderBy(c => c.ExpiresAt)
            .Take(_options.BatchSize)
            .ExecuteDeleteAsync(ct);
    }

    private static TwoFactorCleanupOptions Validate(TwoFactorCleanupOptions o)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(o.RetentionHours);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(o.BatchSize);
        return o;
    }
}
