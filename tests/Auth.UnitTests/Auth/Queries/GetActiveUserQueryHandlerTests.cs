using Auth.Application;
using Auth.Application.Auth.Queries.GetActiveUser;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Authentication.Queries.GetActiveUser;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.Auth.Queries;

public sealed class GetActiveUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_ActiveUser_ReturnsUser()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = new GetActiveUserQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetActiveUserQuery(user.Id),
            CancellationToken.None);

        result.Id.Should().Be(user.Id);
        result.Username.Should().Be("alice");
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsUserInactive()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = false };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = new GetActiveUserQueryHandler(dbContext);

        var act = () => handler.Handle(
            new GetActiveUserQuery(user.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.UserInactive);
    }

    [Fact]
    public async Task Handle_NonExistent_ThrowsUserInactive()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetActiveUserQueryHandler(dbContext);

        var act = () => handler.Handle(
            new GetActiveUserQuery(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.UserInactive);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
