using Auth.Application;
using Auth.Application.Sessions.Commands.CreateSession;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Sessions.Commands.CreateSession;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Sessions.Commands;

public sealed class CreateSessionCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesSessionAndReturnsId()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, out var userId);
        var app = new global::Auth.Domain.Application { Name = "Web App", ClientId = "web-app" };
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var sessionId = await handler.Handle(
            new CreateSessionCommand(userId, "web-app", "pwd", "127.0.0.1", "TestAgent/1.0"),
            CancellationToken.None);

        sessionId.Should().NotBe(Guid.Empty);
        var session = await dbContext.UserSessions.FirstAsync(s => s.Id == sessionId);
        session.UserId.Should().Be(userId);
        session.ApplicationId.Should().Be(app.Id);
        session.AuthMethod.Should().Be("pwd");
        session.IpAddress.Should().Be("127.0.0.1");
        session.UserAgent.Should().Be("TestAgent/1.0");
        session.IsRevoked.Should().BeFalse();
        session.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_WithNullClientId_CreatesSession()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, out var userId);
        var handler = CreateHandler(dbContext);

        var sessionId = await handler.Handle(
            new CreateSessionCommand(userId, null, "pwd", "192.168.1.1", "UA"),
            CancellationToken.None);

        var session = await dbContext.UserSessions.FirstAsync(s => s.Id == sessionId);
        session.IpAddress.Should().Be("192.168.1.1");
        session.UserAgent.Should().Be("UA");
        session.ApplicationId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithKnownClientId_ResolvesApplicationId()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, out var userId);
        var app = new global::Auth.Domain.Application { Name = "Web App", ClientId = "web-app" };
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var sessionId = await handler.Handle(
            new CreateSessionCommand(userId, "web-app", "pwd", "127.0.0.1", "UA"),
            CancellationToken.None);

        var session = await dbContext.UserSessions.FirstAsync(s => s.Id == sessionId);
        session.ApplicationId.Should().Be(app.Id);
    }

    [Fact]
    public async Task Handle_WithUnknownClientId_ThrowsApplicationNotFound()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, out var userId);
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new CreateSessionCommand(userId, "nonexistent-client", "pwd", "127.0.0.1", "UA"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(e => e.Code == AuthErrorCatalog.ApplicationNotFound);
    }

    private static CreateSessionCommandHandler CreateHandler(AuthDbContext dbContext)
    {
        var options = Options.Create(new IntegrationOptions
        {
            Oidc = new OidcOptions { RefreshTokenLifetimeDays = 7 }
        });
        return new CreateSessionCommandHandler(dbContext, options, new Mock<IEventBus>().Object, new Mock<IAuditService>().Object);
    }

    private static void SeedUser(AuthDbContext dbContext, out Guid userId)
    {
        var user = new User { Username = "test", Email = "test@test.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        dbContext.SaveChanges();
        userId = user.Id;
    }
}
