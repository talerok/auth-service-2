using Auth.Application;
using Auth.Application.Sessions.Commands.RevokeOwnSession;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Sessions.Commands.RevokeOwnSession;
using FluentAssertions;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Sessions.Commands;

public sealed class RevokeOwnSessionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_RevokesWithLogoutReason()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = UserSession.Create(userId, "127.0.0.1", "UA", null, "pwd", 7);
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        await handler.Handle(
            new RevokeOwnSessionCommand(session.Id, userId), CancellationToken.None);

        session.IsRevoked.Should().BeTrue();
        session.RevokedReason.Should().Be("logout");
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ThrowsSessionNotFound()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new RevokeOwnSessionCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionNotFound);
    }

    [Fact]
    public async Task Handle_WhenUserIdMismatch_ThrowsSessionNotFound()
    {
        await using var dbContext = CreateDbContext();
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new RevokeOwnSessionCommand(session.Id, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionNotFound);
    }

    [Fact]
    public async Task Handle_WhenAlreadyRevoked_ThrowsSessionAlreadyRevoked()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = UserSession.Create(userId, "127.0.0.1", "UA", null, "pwd", 7);
        session.Revoke("first");
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new RevokeOwnSessionCommand(session.Id, userId),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionAlreadyRevoked);
    }

    private static RevokeOwnSessionCommandHandler CreateHandler(AuthDbContext dbContext) =>
        new(dbContext, new Mock<IEventBus>().Object, new Mock<IAuditService>().Object);
}
