using Auth.Application;
using Auth.Application.Users.Commands.CreateUser;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Users.Commands.CreateUser;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests.Users.Commands;

public sealed class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenTwoFactorEnabled_SetsEnabledWithChannel()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        var handler = CreateHandler(dbContext, hasher);

        var result = await handler.Handle(
            new CreateUserCommand("bob", "Bob", "bob@example.com", "pwd",
                TwoFactorEnabled: true, TwoFactorChannel: TwoFactorChannel.Email),
            CancellationToken.None);

        result.TwoFactorEnabled.Should().BeTrue();
        result.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);

        var user = await dbContext.Users.FirstAsync(x => x.Id == result.Id);
        user.TwoFactorEnabled.Should().BeTrue();
        user.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task Handle_WhenTwoFactorEnabledWithoutChannel_DefaultsToEmail()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        var handler = CreateHandler(dbContext, hasher);

        var result = await handler.Handle(
            new CreateUserCommand("bob", "Bob", "bob@example.com", "pwd",
                TwoFactorEnabled: true, TwoFactorChannel: null),
            CancellationToken.None);

        result.TwoFactorEnabled.Should().BeTrue();
        result.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task Handle_WhenTwoFactorDisabled_LeavesDisabled()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        var handler = CreateHandler(dbContext, hasher);

        var result = await handler.Handle(
            new CreateUserCommand("bob", "Bob", "bob@example.com", "pwd", TwoFactorEnabled: false),
            CancellationToken.None);

        result.TwoFactorEnabled.Should().BeFalse();
        result.TwoFactorChannel.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithPhone_SetsPhone()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        var handler = CreateHandler(dbContext, hasher);

        var result = await handler.Handle(
            new CreateUserCommand("bob", "Bob", "bob@example.com", "pwd", Phone: "+1234567890"),
            CancellationToken.None);

        result.Phone.Should().Be("+1234567890");

        var user = await dbContext.Users.FirstAsync(x => x.Id == result.Id);
        user.Phone.Should().Be("+1234567890");
    }

    private static CreateUserCommandHandler CreateHandler(
        AuthDbContext dbContext,
        Mock<IPasswordHasher>? passwordHasher = null)
    {
        passwordHasher ??= new Mock<IPasswordHasher>();
        var searchIndex = new Mock<ISearchIndexService>();
        return new CreateUserCommandHandler(dbContext, passwordHasher.Object, searchIndex.Object);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
