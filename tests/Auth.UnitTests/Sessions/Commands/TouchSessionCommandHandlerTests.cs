using Auth.Application;
using Auth.Application.Sessions.Commands.TouchSession;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Sessions.Commands.TouchSession;
using FluentAssertions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Sessions.Commands;

public sealed class TouchSessionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenActiveSession_UpdatesLastActivityAt()
    {
        await using var dbContext = CreateDbContext();
        var session = SeedActiveSession(dbContext);
        var before = session.LastActivityAt;
        var handler = new TouchSessionCommandHandler(dbContext);

        await handler.Handle(new TouchSessionCommand(session.Id, session.UserId), CancellationToken.None);

        session.LastActivityAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ThrowsSessionRevoked()
    {
        await using var dbContext = CreateDbContext();
        var handler = new TouchSessionCommandHandler(dbContext);

        var act = () => handler.Handle(
            new TouchSessionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionRevoked);
    }

    [Fact]
    public async Task Handle_WhenUserIdMismatch_ThrowsSessionRevoked()
    {
        await using var dbContext = CreateDbContext();
        var session = SeedActiveSession(dbContext);
        var handler = new TouchSessionCommandHandler(dbContext);

        var act = () => handler.Handle(
            new TouchSessionCommand(session.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionRevoked);
    }

    [Fact]
    public async Task Handle_WhenSessionRevoked_ThrowsSessionRevoked()
    {
        await using var dbContext = CreateDbContext();
        var session = SeedActiveSession(dbContext);
        session.Revoke("test");
        await dbContext.SaveChangesAsync();
        var handler = new TouchSessionCommandHandler(dbContext);

        var act = () => handler.Handle(
            new TouchSessionCommand(session.Id, session.UserId), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionRevoked);
    }

    [Fact]
    public async Task Handle_WhenSessionExpired_ThrowsSessionRevoked()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = UserSession.Create(userId, "127.0.0.1", "UA", null, "pwd", 7);
        session.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();
        var handler = new TouchSessionCommandHandler(dbContext);

        var act = () => handler.Handle(
            new TouchSessionCommand(session.Id, userId), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionRevoked);
    }

    private static UserSession SeedActiveSession(AuthDbContext dbContext)
    {
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);
        dbContext.UserSessions.Add(session);
        dbContext.SaveChanges();
        return session;
    }
}
