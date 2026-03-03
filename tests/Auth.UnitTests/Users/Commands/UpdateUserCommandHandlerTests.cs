using Auth.Application;
using Auth.Application.Users.Commands.UpdateUser;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Users.Commands.UpdateUser;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests.Users.Commands;

public sealed class UpdateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenTwoFactorEnabled_SetsEnabledWithChannel()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new UpdateUserCommand(user.Id, "alice", "Alice", "alice@example.com", null, true,
                TwoFactorEnabled: true, TwoFactorChannel: TwoFactorChannel.Email),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.TwoFactorEnabled.Should().BeTrue();
        result.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task Handle_WhenTwoFactorDisabled_DisablesTwoFactor()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new UpdateUserCommand(user.Id, "alice", "Alice", "alice@example.com", null, true,
                TwoFactorEnabled: false),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.TwoFactorEnabled.Should().BeFalse();
        result.TwoFactorChannel.Should().BeNull();
    }

    private static UpdateUserCommandHandler CreateHandler(AuthDbContext dbContext)
    {
        var searchIndex = new Mock<ISearchIndexService>();
        return new UpdateUserCommandHandler(dbContext, searchIndex.Object);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
