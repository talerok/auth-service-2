using Auth.Application;
using Auth.Domain;
using Auth.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Auth.UnitTests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_WhenUserAlreadyExists_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var act = () => service.RegisterAsync(new RegisterRequest("admin", "admin", "new@example.com", "pwd"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.DuplicateIdentity);
    }

    [Fact]
    public async Task RegisterAsync_WhenValid_CreatesUserAndIndexesDocument()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash("pwd")).Returns("hashed");
        var searchIndex = new Mock<ISearchIndexService>();
        var service = CreateService(dbContext, hasher: hasher, searchIndexService: searchIndex);

        var result = await service.RegisterAsync(new RegisterRequest("new-user", "new-user", "new@example.com", "pwd"), CancellationToken.None);

        result.Username.Should().Be("new-user");
        result.Email.Should().Be("new@example.com");
        result.IsActive.Should().BeTrue();
        (await dbContext.Users.CountAsync()).Should().Be(1);
        searchIndex.Verify(x => x.IndexUserAsync(It.Is<UserDto>(u => u.Id == result.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenPasswordIsInvalid_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("wrong", "hash")).Returns(false);
        var service = CreateService(dbContext, hasher: hasher);

        var act = () => service.ValidateCredentialsAsync("admin", "wrong", CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidCredentials);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenValid_ReturnsUser()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("pwd", "hash")).Returns(true);
        var service = CreateService(dbContext, hasher: hasher);

        var result = await service.ValidateCredentialsAsync("admin", "pwd", CancellationToken.None);

        result.Should().NotBeNull();
        result.Username.Should().Be("admin");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenUserIsInactive_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            IsActive = false
        });
        await dbContext.SaveChangesAsync();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("pwd", "hash")).Returns(true);
        var service = CreateService(dbContext, hasher: hasher);

        var act = () => service.ValidateCredentialsAsync("admin", "pwd", CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidCredentials);
    }

    [Fact]
    public async Task GetActiveUserAsync_WhenUserExists_ReturnsUser()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetActiveUserAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetActiveUserAsync_WhenUserIsInactive_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            IsActive = false
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var act = () => service.GetActiveUserAsync(user.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.UserInactive);
    }

    [Fact]
    public async Task ValidateForcedPasswordChangeAsync_WithValidChallenge_UpdatesPasswordHash()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "old-hash", IsActive = true };
        user.MarkMustChangePassword();
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash("new-password")).Returns("new-hash");
        var service = CreateService(dbContext, hasher: hasher);

        await service.ValidateForcedPasswordChangeAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        user.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public async Task ValidateForcedPasswordChangeAsync_WithValidChallenge_ClearsMustChangePasswordFlag()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.MarkMustChangePassword();
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.ValidateForcedPasswordChangeAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        user.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateForcedPasswordChangeAsync_WithValidChallenge_ReturnsUser()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.MarkMustChangePassword();
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.ValidateForcedPasswordChangeAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task ValidateForcedPasswordChangeAsync_WithValidChallenge_MarksChallengeAsUsed()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.MarkMustChangePassword();
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.ValidateForcedPasswordChangeAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        challenge.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateForcedPasswordChangeAsync_WithExpiredChallenge_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(1));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();
        dbContext.Entry(challenge).Property("ExpiresAt").CurrentValue = DateTime.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var act = () => service.ValidateForcedPasswordChangeAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidPasswordChangeChallenge);
    }

    [Fact]
    public async Task ValidateForcedPasswordChangeAsync_WithAlreadyUsedChallenge_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        challenge.MarkAsUsed();
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var act = () => service.ValidateForcedPasswordChangeAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidPasswordChangeChallenge);
    }

    [Fact]
    public async Task ValidateForcedPasswordChangeAsync_WithNonExistentChallengeId_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var act = () => service.ValidateForcedPasswordChangeAsync(new ForcedPasswordChangeRequest(Guid.NewGuid(), "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidPasswordChangeChallenge);
    }

    [Fact]
    public async Task ValidateForcedPasswordChangeAsync_WhenUserIsInactive_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = false };
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var act = () => service.ValidateForcedPasswordChangeAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.UserInactive);
    }

    [Fact]
    public async Task CreateLoginChallengeAsync_WhenCalled_CreatesTwoFactorChallenge()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.CreateLoginChallengeAsync(user.Id, TwoFactorChannel.Email, CancellationToken.None);

        result.Should().NotBeNull();
        result.UserId.Should().Be(user.Id);
        result.Channel.Should().Be(TwoFactorChannel.Email);
        (await dbContext.TwoFactorChallenges.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreatePasswordChangeChallengeAsync_CreatesAndPersistsChallenge()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.CreatePasswordChangeChallengeAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.UserId.Should().Be(user.Id);
        (await dbContext.PasswordChangeChallenges.CountAsync()).Should().Be(1);
    }

    private static AuthService CreateService(
        AuthDbContext dbContext,
        Mock<IPasswordHasher>? hasher = null,
        Mock<ISearchIndexService>? searchIndexService = null)
    {
        var hasCustomHasher = hasher is not null;
        hasher ??= new Mock<IPasswordHasher>();
        if (!hasCustomHasher)
        {
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
            hasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        }

        var hasCustomSearchIndex = searchIndexService is not null;
        searchIndexService ??= new Mock<ISearchIndexService>();
        if (!hasCustomSearchIndex)
        {
            searchIndexService.Setup(x => x.IndexUserAsync(It.IsAny<UserDto>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        var options = Options.Create(new IntegrationOptions
        {
            Jwt = new JwtOptions
            {
                Secret = "super-secret-key-min-32-characters-long!"
            }
        });

        return new AuthService(
            dbContext,
            hasher.Object,
            searchIndexService.Object,
            options,
            NullLogger<AuthService>.Instance);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuthDbContext(options);
    }
}
