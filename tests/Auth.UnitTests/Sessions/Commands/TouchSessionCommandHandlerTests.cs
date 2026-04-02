using Auth.Application;
using Auth.Application.Messaging;
using Auth.Application.Sessions.Commands.TouchSession;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Sessions.Commands.TouchSession;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Sessions.Commands;

public sealed class TouchSessionCommandHandlerTests
{
    private static IOptions<IntegrationOptions> CreateOptions() =>
        Options.Create(new IntegrationOptions { Oidc = new OidcOptions { RefreshTokenLifetimeDays = 7 } });

    [Fact]
    public async Task Handle_WhenActiveSession_UpdatesLastActivityAtAndExpiresAt()
    {
        await using var dbContext = CreateDbContext();
        var (session, _) = SeedActiveSessionWithUser(dbContext);
        var handler = new TouchSessionCommandHandler(dbContext, CreateOptions(), new Mock<IEventBus>().Object);

        await handler.Handle(new TouchSessionCommand(session.Id, session.UserId), CancellationToken.None);

        session.LastActivityAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        session.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ThrowsSessionRevoked()
    {
        await using var dbContext = CreateDbContext();
        var handler = new TouchSessionCommandHandler(dbContext, CreateOptions(), new Mock<IEventBus>().Object);

        var act = () => handler.Handle(
            new TouchSessionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionRevoked);
    }

    [Fact]
    public async Task Handle_WhenUserIdMismatch_ThrowsSessionRevoked()
    {
        await using var dbContext = CreateDbContext();
        var (session, _) = SeedActiveSessionWithUser(dbContext);
        var handler = new TouchSessionCommandHandler(dbContext, CreateOptions(), new Mock<IEventBus>().Object);

        var act = () => handler.Handle(
            new TouchSessionCommand(session.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionRevoked);
    }

    [Fact]
    public async Task Handle_WhenSessionRevoked_ThrowsSessionRevoked()
    {
        await using var dbContext = CreateDbContext();
        var (session, _) = SeedActiveSessionWithUser(dbContext);
        session.Revoke("test");
        await dbContext.SaveChangesAsync();
        var handler = new TouchSessionCommandHandler(dbContext, CreateOptions(), new Mock<IEventBus>().Object);

        var act = () => handler.Handle(
            new TouchSessionCommand(session.Id, session.UserId), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionRevoked);
    }

    [Fact]
    public async Task Handle_WhenSessionExpired_ThrowsSessionRevoked()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@test.com", PasswordHash = "h", IsActive = true };
        dbContext.Users.Add(user);
        var session = UserSession.Create(user.Id, "127.0.0.1", "UA", null, "pwd", 7);
        session.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();
        var handler = new TouchSessionCommandHandler(dbContext, CreateOptions(), new Mock<IEventBus>().Object);

        var act = () => handler.Handle(
            new TouchSessionCommand(session.Id, user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionRevoked);
    }

    [Fact]
    public async Task Handle_WhenUserDeleted_RevokesSessionAndThrowsSessionRevoked()
    {
        await using var dbContext = CreateDbContext();
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();
        var handler = new TouchSessionCommandHandler(dbContext, CreateOptions(), new Mock<IEventBus>().Object);

        var act = () => handler.Handle(
            new TouchSessionCommand(session.Id, session.UserId), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.SessionRevoked);
        session.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserLockedOut_RevokesSessionAndThrowsAccountLockedOut()
    {
        await using var dbContext = CreateDbContext();
        var (session, user) = SeedActiveSessionWithUser(dbContext);
        user.RegisterFailedLogin(1, 15);
        await dbContext.SaveChangesAsync();
        var handler = new TouchSessionCommandHandler(dbContext, CreateOptions(), new Mock<IEventBus>().Object);

        var act = () => handler.Handle(
            new TouchSessionCommand(session.Id, session.UserId), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.AccountLockedOut);
        session.IsRevoked.Should().BeTrue();
    }

    private static (UserSession session, User user) SeedActiveSessionWithUser(AuthDbContext dbContext)
    {
        var user = new User { Username = "alice", Email = "alice@test.com", PasswordHash = "h", IsActive = true };
        dbContext.Users.Add(user);
        var session = UserSession.Create(user.Id, "127.0.0.1", "UA", null, "pwd", 7);
        dbContext.UserSessions.Add(session);
        dbContext.SaveChanges();
        return (session, user);
    }
}
