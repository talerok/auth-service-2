using Auth.Domain;
using Auth.Infrastructure.DistributedJobs.Cleanup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.IntegrationTests.Cleanup;

[Collection("Integration")]
public sealed class CleanupJobTests(IntegrationTestFixture fixture)
{
    private static IOptions<IntegrationOptions> CreateOptions(Action<CleanupOptions>? configure = null)
    {
        var options = new IntegrationOptions();
        configure?.Invoke(options.Cleanup);
        return Options.Create(options);
    }

    // --- SessionCleanupJob ---

    [Fact]
    public async Task SessionCleanupJob_DeletesExpiredSessions()
    {
        var userId = await fixture.ExecuteDbAsync(async db =>
        {
            var user = new User { Username = $"cleanup-{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@test.local", FullName = "Cleanup Test", IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Id;
        });

        await fixture.ExecuteDbAsync(async db =>
        {
            var expired = UserSession.Create(userId, "127.0.0.1", "test-agent", null, "password", 1);
            expired.ExpiresAt = DateTime.UtcNow.AddDays(-30);

            var active = UserSession.Create(userId, "127.0.0.1", "test-agent", null, "password", 7);

            db.UserSessions.AddRange(expired, active);
            await db.SaveChangesAsync();
        });

        var deleted = await fixture.ExecuteDbAsync(async db =>
        {
            var job = new SessionCleanupJob(db, CreateOptions());
            return await job.ExecuteAsync(CancellationToken.None);
        });

        deleted.Should().Be(1);

        var remainingCount = await fixture.ExecuteDbAsync(db =>
            db.UserSessions.CountAsync(s => s.UserId == userId));
        remainingCount.Should().Be(1);
    }

    [Fact]
    public async Task SessionCleanupJob_DeletesRevokedSessions()
    {
        var userId = await fixture.ExecuteDbAsync(async db =>
        {
            var user = new User { Username = $"cleanup-{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@test.local", FullName = "Cleanup Test", IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Id;
        });

        await fixture.ExecuteDbAsync(async db =>
        {
            var revoked = UserSession.Create(userId, "127.0.0.1", "test-agent", null, "password", 1);
            revoked.Revoke("test");
            // Simulate old revocation by setting ExpiresAt to past
            revoked.ExpiresAt = DateTime.UtcNow.AddDays(-30);

            db.UserSessions.Add(revoked);
            await db.SaveChangesAsync();
        });

        var deleted = await fixture.ExecuteDbAsync(async db =>
        {
            var job = new SessionCleanupJob(db, CreateOptions());
            return await job.ExecuteAsync(CancellationToken.None);
        });

        deleted.Should().Be(1);
    }

    [Fact]
    public async Task SessionCleanupJob_RespectsRetentionPeriod()
    {
        var userId = await fixture.ExecuteDbAsync(async db =>
        {
            var user = new User { Username = $"cleanup-{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@test.local", FullName = "Cleanup Test", IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Id;
        });

        Guid sessionId = default;
        await fixture.ExecuteDbAsync(async db =>
        {
            // Expired 2 days ago — within 7-day retention
            var recentlyExpired = UserSession.Create(userId, "127.0.0.1", "test-agent", null, "password", 1);
            recentlyExpired.ExpiresAt = DateTime.UtcNow.AddDays(-2);
            sessionId = recentlyExpired.Id;

            db.UserSessions.Add(recentlyExpired);
            await db.SaveChangesAsync();
        });

        await fixture.ExecuteDbAsync(async db =>
        {
            var job = new SessionCleanupJob(db, CreateOptions());
            await job.ExecuteAsync(CancellationToken.None);
        });

        var sessionExists = await fixture.ExecuteDbAsync(db =>
            db.UserSessions.AnyAsync(s => s.Id == sessionId));
        sessionExists.Should().BeTrue("session within retention period should not be deleted");
    }

    // --- TwoFactorChallengeCleanupJob ---

    [Fact]
    public async Task TwoFactorChallengeCleanupJob_DeletesExpiredChallenges()
    {
        var userId = await fixture.ExecuteDbAsync(async db =>
        {
            var user = new User { Username = $"cleanup-{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@test.local", FullName = "Cleanup Test", IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Id;
        });

        await fixture.ExecuteDbAsync(async db =>
        {
            var expired = TwoFactorChallenge.Create(
                userId, TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email,
                "hash", "salt", "encrypted", DateTime.UtcNow.AddDays(-2), 5);

            var fresh = TwoFactorChallenge.Create(
                userId, TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email,
                "hash2", "salt2", "encrypted2", DateTime.UtcNow.AddMinutes(5), 5);

            db.TwoFactorChallenges.AddRange(expired, fresh);
            await db.SaveChangesAsync();
        });

        var deleted = await fixture.ExecuteDbAsync(async db =>
        {
            var job = new TwoFactorChallengeCleanupJob(db, CreateOptions());
            return await job.ExecuteAsync(CancellationToken.None);
        });

        deleted.Should().Be(1);

        var remainingCount = await fixture.ExecuteDbAsync(db =>
            db.TwoFactorChallenges.CountAsync(c => c.UserId == userId));
        remainingCount.Should().Be(1);
    }

    // --- PasswordChallengeCleanupJob ---

    [Fact]
    public async Task PasswordChallengeCleanupJob_DeletesExpiredChallenges()
    {
        var userId = await fixture.ExecuteDbAsync(async db =>
        {
            var user = new User { Username = $"cleanup-{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@test.local", FullName = "Cleanup Test", IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Id;
        });

        await fixture.ExecuteDbAsync(async db =>
        {
            var expired = PasswordChangeChallenge.Create(userId, DateTime.UtcNow.AddDays(1));
            // Force expire it by modifying the entity directly through EF
            db.PasswordChangeChallenges.Add(expired);
            await db.SaveChangesAsync();

            await db.PasswordChangeChallenges
                .Where(c => c.Id == expired.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.ExpiresAt, DateTime.UtcNow.AddDays(-2)));
        });

        var deleted = await fixture.ExecuteDbAsync(async db =>
        {
            var job = new PasswordChallengeCleanupJob(db, CreateOptions());
            return await job.ExecuteAsync(CancellationToken.None);
        });

        deleted.Should().Be(1);
    }

    // --- AuditLogCleanupJob ---

    [Fact]
    public async Task AuditLogCleanupJob_DeletesOldEntries()
    {
        await fixture.ExecuteDbAsync(async db =>
        {
            var old = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow.AddDays(-400),
                EntityType = AuditEntityType.User,
                EntityId = Guid.NewGuid(),
                Action = AuditAction.Create,
                ActorType = AuditActorType.System
            };

            var recent = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow.AddDays(-30),
                EntityType = AuditEntityType.User,
                EntityId = Guid.NewGuid(),
                Action = AuditAction.Create,
                ActorType = AuditActorType.System
            };

            db.AuditLogEntries.AddRange(old, recent);
            await db.SaveChangesAsync();
        });

        var deleted = await fixture.ExecuteDbAsync(async db =>
        {
            var job = new AuditLogCleanupJob(db, CreateOptions());
            return await job.ExecuteAsync(CancellationToken.None);
        });

        deleted.Should().BeGreaterThanOrEqualTo(1);
    }

    // --- Batch size ---

    [Fact]
    public async Task SessionCleanupJob_RespectsBatchSize()
    {
        var userId = await fixture.ExecuteDbAsync(async db =>
        {
            var user = new User { Username = $"cleanup-{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@test.local", FullName = "Cleanup Test", IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Id;
        });

        await fixture.ExecuteDbAsync(async db =>
        {
            var sessions = Enumerable.Range(0, 5).Select(_ =>
            {
                var s = UserSession.Create(userId, "127.0.0.1", "test-agent", null, "password", 1);
                s.ExpiresAt = DateTime.UtcNow.AddDays(-30);
                return s;
            });
            db.UserSessions.AddRange(sessions);
            await db.SaveChangesAsync();
        });

        var deleted = await fixture.ExecuteDbAsync(async db =>
        {
            var options = CreateOptions(c => c.Sessions.BatchSize = 3);
            var job = new SessionCleanupJob(db, options);
            return await job.ExecuteAsync(CancellationToken.None);
        });

        deleted.Should().BeLessThanOrEqualTo(3, "batch size should cap deletions per iteration");
        deleted.Should().BeGreaterThan(0);

        var remaining = await fixture.ExecuteDbAsync(db =>
            db.UserSessions.CountAsync(s => s.UserId == userId));
        remaining.Should().BeGreaterThanOrEqualTo(2, "at least 2 of 5 sessions should survive one batch");
    }
}
