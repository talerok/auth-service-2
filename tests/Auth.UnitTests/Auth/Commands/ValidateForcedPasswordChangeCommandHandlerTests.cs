using Auth.Application;
using Auth.Application.Auth.Commands.ValidateForcedPasswordChange;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Authentication.Commands.ValidateForcedPasswordChange;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Auth.Commands;

public sealed class ValidateForcedPasswordChangeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidChallenge_UpdatesPasswordAndReturnsUser()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash("newPassword")).Returns("hashed_new");
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "old_hash", IsActive = true };
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();
        var handler = new ValidateForcedPasswordChangeCommandHandler(
            dbContext, hasher.Object, NullLogger<ValidateForcedPasswordChangeCommandHandler>.Instance);

        var result = await handler.Handle(
            new ValidateForcedPasswordChangeCommand(challenge.Id, "newPassword"),
            CancellationToken.None);

        result.Id.Should().Be(user.Id);
        var updated = await dbContext.Users.FirstAsync(x => x.Id == user.Id);
        updated.PasswordHash.Should().Be("hashed_new");
    }

    [Fact]
    public async Task Handle_ValidChallenge_ClearsMustChangePassword()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "old_hash", IsActive = true };
        user.MarkMustChangePassword();
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();
        var handler = new ValidateForcedPasswordChangeCommandHandler(
            dbContext, hasher.Object, NullLogger<ValidateForcedPasswordChangeCommandHandler>.Instance);

        await handler.Handle(
            new ValidateForcedPasswordChangeCommand(challenge.Id, "newPassword"),
            CancellationToken.None);

        var updated = await dbContext.Users.FirstAsync(x => x.Id == user.Id);
        updated.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidChallenge_MarksChallengeAsUsed()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "old_hash", IsActive = true };
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();
        var handler = new ValidateForcedPasswordChangeCommandHandler(
            dbContext, hasher.Object, NullLogger<ValidateForcedPasswordChangeCommandHandler>.Instance);

        await handler.Handle(
            new ValidateForcedPasswordChangeCommand(challenge.Id, "newPassword"),
            CancellationToken.None);

        var updated = await dbContext.PasswordChangeChallenges.FirstAsync(x => x.Id == challenge.Id);
        updated.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExpiredChallenge_Throws()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "old_hash", IsActive = true };
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        // Force expiration by waiting — instead, we create a challenge that's already about to expire
        // We need to use reflection or a different approach since ExpiresAt is private set
        // The handler checks IsExpired(DateTime.UtcNow), so we need to set ExpiresAt in the past
        // Since PasswordChangeChallenge.Create throws if expiresAt <= UtcNow, we manipulate via EF
        dbContext.Entry(challenge).Property("ExpiresAt").CurrentValue = DateTime.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();

        var handler = new ValidateForcedPasswordChangeCommandHandler(
            dbContext, hasher.Object, NullLogger<ValidateForcedPasswordChangeCommandHandler>.Instance);

        var act = () => handler.Handle(
            new ValidateForcedPasswordChangeCommand(challenge.Id, "newPassword"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidPasswordChangeChallenge);
    }

    [Fact]
    public async Task Handle_UsedChallenge_Throws()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "old_hash", IsActive = true };
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        challenge.MarkAsUsed();
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();
        var handler = new ValidateForcedPasswordChangeCommandHandler(
            dbContext, hasher.Object, NullLogger<ValidateForcedPasswordChangeCommandHandler>.Instance);

        var act = () => handler.Handle(
            new ValidateForcedPasswordChangeCommand(challenge.Id, "newPassword"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidPasswordChangeChallenge);
    }

    [Fact]
    public async Task Handle_NonExistentChallenge_Throws()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        var handler = new ValidateForcedPasswordChangeCommandHandler(
            dbContext, hasher.Object, NullLogger<ValidateForcedPasswordChangeCommandHandler>.Instance);

        var act = () => handler.Handle(
            new ValidateForcedPasswordChangeCommand(Guid.NewGuid(), "newPassword"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidPasswordChangeChallenge);
    }

    [Fact]
    public async Task Handle_InactiveUser_Throws()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "old_hash", IsActive = false };
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();
        var handler = new ValidateForcedPasswordChangeCommandHandler(
            dbContext, hasher.Object, NullLogger<ValidateForcedPasswordChangeCommandHandler>.Instance);

        var act = () => handler.Handle(
            new ValidateForcedPasswordChangeCommand(challenge.Id, "newPassword"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.UserInactive);
    }

}
