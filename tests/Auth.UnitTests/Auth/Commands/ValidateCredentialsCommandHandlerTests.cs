using Auth.Application;
using Auth.Application.Auth.Commands.ValidateCredentials;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Authentication.Commands.ValidateCredentials;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Auth.Commands;

public sealed class ValidateCredentialsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidPassword_ReturnsUser()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("correctPassword", "hashed")).Returns(true);
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hashed", IsActive = true, IsInternalAuthEnabled = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var auditContext = new Mock<IAuditContext>();
        var handler = new ValidateCredentialsCommandHandler(dbContext, hasher.Object, auditContext.Object);

        var result = await handler.Handle(
            new ValidateCredentialsCommand("alice", "correctPassword"),
            CancellationToken.None);

        result.Id.Should().Be(user.Id);
        result.Username.Should().Be("alice");
    }

    [Fact]
    public async Task Handle_InternalAuthDisabled_ThrowsInternalAuthDisabled()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("correctPassword", "hashed")).Returns(true);
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hashed", IsActive = true, IsInternalAuthEnabled = false };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var auditContext = new Mock<IAuditContext>();
        var handler = new ValidateCredentialsCommandHandler(dbContext, hasher.Object, auditContext.Object);

        var act = () => handler.Handle(
            new ValidateCredentialsCommand("alice", "correctPassword"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InternalAuthDisabled);
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
        var auditContext = new Mock<IAuditContext>();
        var handler = new ValidateCredentialsCommandHandler(dbContext, hasher.Object, auditContext.Object);

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
        var auditContext = new Mock<IAuditContext>();
        var handler = new ValidateCredentialsCommandHandler(dbContext, hasher.Object, auditContext.Object);

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
        var auditContext = new Mock<IAuditContext>();
        var handler = new ValidateCredentialsCommandHandler(dbContext, hasher.Object, auditContext.Object);

        var act = () => handler.Handle(
            new ValidateCredentialsCommand("nonexistent", "anyPassword"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_InvalidCredentials_SetsFailureAuditDetails()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        var auditContext = new Mock<IAuditContext>();
        auditContext.SetupAllProperties();
        var handler = new ValidateCredentialsCommandHandler(dbContext, hasher.Object, auditContext.Object);

        var act = () => handler.Handle(
            new ValidateCredentialsCommand("nonexistent", "anyPassword"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>();
        auditContext.Object.Details.Should().NotBeNull();
        auditContext.Object.Details!["username"].Should().Be("nonexistent");
        auditContext.Object.Details!["result"].Should().Be("failure");
    }

    [Fact]
    public async Task Handle_ValidCredentials_SetsSuccessAuditDetails()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("correctPassword", "hashed")).Returns(true);
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hashed", IsActive = true, IsInternalAuthEnabled = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var auditContext = new Mock<IAuditContext>();
        auditContext.SetupAllProperties();
        var handler = new ValidateCredentialsCommandHandler(dbContext, hasher.Object, auditContext.Object);

        await handler.Handle(
            new ValidateCredentialsCommand("alice", "correctPassword"),
            CancellationToken.None);

        auditContext.Object.Details.Should().NotBeNull();
        auditContext.Object.Details!["username"].Should().Be("alice");
        auditContext.Object.Details!["result"].Should().Be("success");
        auditContext.Object.EntityId.Should().Be(user.Id);
    }

}
