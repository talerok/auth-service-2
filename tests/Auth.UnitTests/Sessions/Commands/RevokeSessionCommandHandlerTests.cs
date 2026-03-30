using Auth.Application;
using Auth.Application.Sessions.Commands.RevokeSession;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Sessions.Commands.RevokeSession;
using FluentAssertions;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Sessions.Commands;

public sealed class RevokeSessionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_RevokesSession()
    {
        await using var dbContext = CreateDbContext();
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        await handler.Handle(
            new RevokeSessionCommand(session.Id, "admin"), CancellationToken.None);

        session.IsRevoked.Should().BeTrue();
        session.RevokedReason.Should().Be("admin");
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ThrowsSessionNotFound()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new RevokeSessionCommand(Guid.NewGuid(), "admin"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionNotFound);
    }

    [Fact]
    public async Task Handle_WhenAlreadyRevoked_ThrowsSessionAlreadyRevoked()
    {
        await using var dbContext = CreateDbContext();
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);
        session.Revoke("first");
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new RevokeSessionCommand(session.Id, "second"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionAlreadyRevoked);
    }

    private static RevokeSessionCommandHandler CreateHandler(AuthDbContext dbContext) =>
        new(dbContext, new Mock<IEventBus>().Object, new Mock<IAuditService>().Object);
}
