using Auth.Application;
using Auth.Application.Users.Commands.PatchUser;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Users.Commands.PatchUser;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Users.Commands;

public sealed class PatchUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenTwoFactorEnabledWithChannel_SetsChannel()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new PatchUserCommand(user.Id, default, default, default, default, default,
                default, TwoFactorEnabled: true, TwoFactorChannel: TwoFactorChannel.Email, default, default, default, default),
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
            new PatchUserCommand(user.Id, default, default, default, default, default, default, TwoFactorEnabled: false, default, default, default, default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.TwoFactorEnabled.Should().BeFalse();
        result.TwoFactorChannel.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTwoFactorNotSpecified_DoesNotChangeTwoFactor()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new PatchUserCommand(user.Id, default, default, "newemail@example.com", default, default, default, default, default, default, default, default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Email.Should().Be("newemail@example.com");
        result.TwoFactorEnabled.Should().BeTrue();
        result.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task Handle_WithPhone_UpdatesPhone()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new PatchUserCommand(user.Id, default, default, default, "+9876543210", default, default, default, default, default, default, default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Phone.Should().Be("+9876543210");
    }

    [Fact]
    public async Task Handle_WithoutPhone_DoesNotChangePhone()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", Phone = "+1111111111", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new PatchUserCommand(user.Id, default, default, "newemail@example.com", default, default, default, default, default, default, default, default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Email.Should().Be("newemail@example.com");
        result.Phone.Should().Be("+1111111111");
    }

    [Fact]
    public async Task Handle_WithInternalAuthEnabled_SetsFlag()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true, IsInternalAuthEnabled = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new PatchUserCommand(user.Id, default, default, default, default, default, IsInternalAuthEnabled: false, default, default, default, default, default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsInternalAuthEnabled.Should().BeFalse();

        var updated = await dbContext.Users.FirstAsync(x => x.Id == user.Id);
        updated.IsInternalAuthEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithoutInternalAuth_DoesNotChange()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true, IsInternalAuthEnabled = false };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new PatchUserCommand(user.Id, default, default, "newemail@example.com", default, default, default, default, default, default, default, default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Email.Should().Be("newemail@example.com");
        result.IsInternalAuthEnabled.Should().BeFalse();
    }

    private static PatchUserCommandHandler CreateHandler(AuthDbContext dbContext)
    {
        var eventBus = new Mock<IEventBus>();
        return new PatchUserCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);
    }

}
