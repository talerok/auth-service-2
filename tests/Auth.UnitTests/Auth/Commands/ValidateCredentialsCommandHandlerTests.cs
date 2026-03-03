using Auth.Application;
using Auth.Application.Auth.Commands.ValidateCredentials;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Authentication.Commands.ValidateCredentials;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests.Auth.Commands;

public sealed class ValidateCredentialsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidPassword_ReturnsUser()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("correctPassword", "hashed")).Returns(true);
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hashed", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = new ValidateCredentialsCommandHandler(dbContext, hasher.Object);

        var result = await handler.Handle(
            new ValidateCredentialsCommand("alice", "correctPassword"),
            CancellationToken.None);

        result.Id.Should().Be(user.Id);
        result.Username.Should().Be("alice");
    }

    [Fact]
    public async Task Handle_InvalidPassword_ThrowsInvalidCredentials()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("wrongPassword", "hashed")).Returns(false);
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hashed", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = new ValidateCredentialsCommandHandler(dbContext, hasher.Object);

        var act = () => handler.Handle(
            new ValidateCredentialsCommand("alice", "wrongPassword"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsInvalidCredentials()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hashed", IsActive = false };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = new ValidateCredentialsCommandHandler(dbContext, hasher.Object);

        var act = () => handler.Handle(
            new ValidateCredentialsCommand("alice", "anyPassword"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsInvalidCredentials()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        var handler = new ValidateCredentialsCommandHandler(dbContext, hasher.Object);

        var act = () => handler.Handle(
            new ValidateCredentialsCommand("nonexistent", "anyPassword"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidCredentials);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
