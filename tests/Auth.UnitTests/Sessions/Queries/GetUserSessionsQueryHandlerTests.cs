using Auth.Application.Sessions.Queries.GetUserSessions;
using Auth.Domain;
using Auth.Infrastructure.Sessions.Queries.GetUserSessions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Sessions.Queries;

public sealed class GetUserSessionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsNonExpiredSessions()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var active = UserSession.Create(userId, "127.0.0.1", "UA1", Guid.NewGuid(), "pwd", 7);
        var expired = UserSession.Create(userId, "10.0.0.1", "UA2", null, "pwd", 7);
        expired.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        dbContext.UserSessions.AddRange(active, expired);
        await dbContext.SaveChangesAsync();
        var handler = new GetUserSessionsQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetUserSessionsQuery(userId), CancellationToken.None);

        result.Should().HaveCount(1);
        var first = result.First();
        first.Id.Should().Be(active.Id);
        first.IpAddress.Should().Be("127.0.0.1");
        first.AuthMethod.Should().Be("pwd");
        first.IsCurrent.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SetsIsCurrentForMatchingSessionId()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var session1 = UserSession.Create(userId, "127.0.0.1", "UA1", null, "pwd", 7);
        var session2 = UserSession.Create(userId, "10.0.0.1", "UA2", null, "pwd", 7);
        dbContext.UserSessions.AddRange(session1, session2);
        await dbContext.SaveChangesAsync();
        var handler = new GetUserSessionsQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetUserSessionsQuery(userId, session1.Id), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Single(r => r.Id == session1.Id).IsCurrent.Should().BeTrue();
        result.Single(r => r.Id == session2.Id).IsCurrent.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DoesNotReturnOtherUsersSessions()
    {
        await using var dbContext = CreateDbContext();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        dbContext.UserSessions.Add(UserSession.Create(userId1, "127.0.0.1", "UA", null, "pwd", 7));
        dbContext.UserSessions.Add(UserSession.Create(userId2, "10.0.0.1", "UA", null, "pwd", 7));
        await dbContext.SaveChangesAsync();
        var handler = new GetUserSessionsQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetUserSessionsQuery(userId1), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().UserId.Should().Be(userId1);
    }

    [Fact]
    public async Task Handle_IncludesRevokedButNotExpiredSessions()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = UserSession.Create(userId, "127.0.0.1", "UA", null, "pwd", 7);
        session.Revoke("test");
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();
        var handler = new GetUserSessionsQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetUserSessionsQuery(userId), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().IsRevoked.Should().BeTrue();
        result.First().RevokedReason.Should().Be("test");
    }

    [Fact]
    public async Task Handle_ReturnsApplicationName()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var app = new global::Auth.Domain.Application { Name = "Portal", ClientId = "portal" };
        dbContext.Applications.Add(app);
        var session = UserSession.Create(userId, "127.0.0.1", "UA", app.Id, "pwd", 7);
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();
        var handler = new GetUserSessionsQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetUserSessionsQuery(userId), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().ApplicationId.Should().Be(app.Id);
        result.First().ApplicationName.Should().Be("Portal");
    }

    [Fact]
    public async Task Handle_WithNullApplicationId_ReturnsNullApplicationName()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = UserSession.Create(userId, "127.0.0.1", "UA", null, "pwd", 7);
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();
        var handler = new GetUserSessionsQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetUserSessionsQuery(userId), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().ApplicationId.Should().BeNull();
        result.First().ApplicationName.Should().BeNull();
    }
}
