using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.DistributedJobs.Cleanup;

internal sealed class PasswordChallengeCleanupJob(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options) : IDistributedJob
{
    private readonly PasswordChallengeCleanupOptions _options = Validate(options.Value.Cleanup.PasswordChangeChallenges);

    public string LockResource => "cleanup:password-change-challenges";
    public TimeSpan Interval => TimeSpan.FromMinutes(_options.IntervalMinutes);
    public int MaxBatchIterations => 100;

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddHours(-_options.RetentionHours);

        return await dbContext.PasswordChangeChallenges
            .Where(c => c.ExpiresAt < cutoff)
            .OrderBy(c => c.ExpiresAt)
            .Take(_options.BatchSize)
            .ExecuteDeleteAsync(ct);
    }

    private static PasswordChallengeCleanupOptions Validate(PasswordChallengeCleanupOptions o)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(o.RetentionHours);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(o.BatchSize);
        return o;
    }
}
