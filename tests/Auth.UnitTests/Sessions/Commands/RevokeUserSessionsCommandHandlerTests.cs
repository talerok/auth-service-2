using Auth.Application;
using Auth.Application.Sessions.Commands.RevokeUserSessions;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Sessions.Commands.RevokeUserSessions;
using FluentAssertions;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Sessions.Commands;

public sealed class RevokeUserSessionsCommandHandlerTests
{
    [Fact]
    public async Task Handle_RevokesAllActiveSessions()
    {
        await using var dbContext = CreateDbContext();
        var user = SeedUserWithSessions(dbContext, out var sessions);
        var handler = CreateHandler(dbContext);

        await handler.Handle(
            new RevokeUserSessionsCommand(user.Id, "revoke-all"), CancellationToken.None);

        sessions.Should().AllSatisfy(s =>
        {
            s.IsRevoked.Should().BeTrue();
            s.RevokedReason.Should().Be("revoke-all");
        });
    }

    [Fact]
    public async Task Handle_SkipsAlreadyRevokedSessions()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "test", Email = "t@t.com", PasswordHash = "h", IsActive = true };
        dbContext.Users.Add(user);
        var active = UserSession.Create(user.Id, "127.0.0.1", "UA", null, "pwd", 7);
        var revoked = UserSession.Create(user.Id, "127.0.0.1", "UA", null, "pwd", 7);
        revoked.Revoke("old");
        dbContext.UserSessions.AddRange(active, revoked);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        await handler.Handle(
            new RevokeUserSessionsCommand(user.Id, "revoke-all"), CancellationToken.None);

        active.IsRevoked.Should().BeTrue();
        active.RevokedReason.Should().Be("revoke-all");
        revoked.RevokedReason.Should().Be("old");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new RevokeUserSessionsCommand(Guid.NewGuid(), "revoke-all"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>();
    }

    private static RevokeUserSessionsCommandHandler CreateHandler(AuthDbContext dbContext) =>
        new(dbContext, new Mock<IEventBus>().Object, new Mock<IAuditService>().Object);

    private static User SeedUserWithSessions(AuthDbContext dbContext, out List<UserSession> sessions)
    {
        var user = new User { Username = "test", Email = "t@t.com", PasswordHash = "h", IsActive = true };
        dbContext.Users.Add(user);
        sessions = [
            UserSession.Create(user.Id, "127.0.0.1", "UA1", null, "pwd", 7),
            UserSession.Create(user.Id, "10.0.0.1", "UA2", Guid.NewGuid(), "pwd+otp", 7)
        ];
        dbContext.UserSessions.AddRange(sessions);
        dbContext.SaveChanges();
        return user;
    }
}
