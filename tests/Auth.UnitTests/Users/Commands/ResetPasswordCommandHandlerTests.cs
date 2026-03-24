using Auth.Application;
using Auth.Application.Users.Commands.ResetPassword;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Users.Commands.ResetPassword;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Users.Commands;

public sealed class ResetPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserExists_UpdatesPasswordAndSetsMustChangePassword()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash("tempPass123")).Returns("hashed_temp");
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "old_hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = new ResetPasswordCommandHandler(dbContext, hasher.Object);

        var result = await handler.Handle(
            new ResetPasswordCommand(user.Id, "tempPass123"),
            CancellationToken.None);

        result.Should().BeTrue();
        var updated = await dbContext.Users.FirstAsync(x => x.Id == user.Id);
        updated.PasswordHash.Should().Be("hashed_temp");
        updated.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        var handler = new ResetPasswordCommandHandler(dbContext, hasher.Object);

        var result = await handler.Handle(
            new ResetPasswordCommand(Guid.NewGuid(), "tempPass123"),
            CancellationToken.None);

        result.Should().BeFalse();
    }

}
