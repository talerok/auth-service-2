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
    public async Task LoginAsync_WhenPasswordIsInvalid_ThrowsAuthException()
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

        var act = () => service.LoginAsync(new LoginRequest("admin", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidCredentials);
    }

    [Fact]
    public async Task LoginAsync_WhenValid_ReturnsTokensAndStoresRefreshToken()
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
        var tokenFactory = new Mock<IJwtTokenFactory>();
        tokenFactory
            .Setup(x => x.CreateTokens(It.IsAny<User>(), It.IsAny<Dictionary<string, byte[]>>()))
            .Returns(new AuthTokensResponse("access", "refresh-new", DateTime.UtcNow.AddMinutes(15)));
        var service = CreateService(dbContext, hasher: hasher, tokenFactory: tokenFactory);

        var result = await service.LoginAsync(new LoginRequest("admin", "pwd"), CancellationToken.None);

        result.RequiresTwoFactor.Should().BeFalse();
        result.Tokens.Should().NotBeNull();
        result.Tokens!.AccessToken.Should().Be("access");
        result.Tokens.RefreshToken.Should().Be("refresh-new");
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(1);
        (await dbContext.RefreshTokens.SingleAsync()).Token.Should().Be("refresh-new");
    }

    [Fact]
    public async Task LoginAsync_WhenTwoFactorIsEnabled_ReturnsChallengeWithoutTokens()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("pwd", "hash")).Returns(true);
        var service = CreateService(dbContext, hasher: hasher);

        var result = await service.LoginAsync(new LoginRequest("admin", "pwd"), CancellationToken.None);

        result.RequiresTwoFactor.Should().BeTrue();
        result.Tokens.Should().BeNull();
        result.ChallengeId.Should().NotBeNull();
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RefreshAsync_WhenCurrentTokenIsValid_RevokesOldAndCreatesNew()
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

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = "refresh-old",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });
        await dbContext.SaveChangesAsync();

        var tokenFactory = new Mock<IJwtTokenFactory>();
        tokenFactory
            .Setup(x => x.CreateTokens(It.IsAny<User>(), It.IsAny<Dictionary<string, byte[]>>()))
            .Returns(new AuthTokensResponse("access-new", "refresh-new", DateTime.UtcNow.AddMinutes(15)));
        var service = CreateService(dbContext, tokenFactory: tokenFactory);

        var result = await service.RefreshAsync(new RefreshRequest("refresh-old"), CancellationToken.None);

        result.RefreshToken.Should().Be("refresh-new");
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(2);
        var oldToken = await dbContext.RefreshTokens.SingleAsync(x => x.Token == "refresh-old");
        oldToken.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenIsInvalid_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var act = () => service.RefreshAsync(new RefreshRequest("missing-token"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidRefreshToken);
    }

    [Fact]
    public async Task RevokeAsync_WhenTokenExists_SetsRevokedAt()
    {
        await using var dbContext = CreateDbContext();
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "refresh",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.RevokeAsync(new RevokeRequest("refresh"), CancellationToken.None);

        var token = await dbContext.RefreshTokens.SingleAsync();
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_WhenMustChangePassword_ReturnsFlagTrue()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.MarkMustChangePassword();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("pwd", "hash")).Returns(true);
        var service = CreateService(dbContext, hasher: hasher);

        var result = await service.LoginAsync(new LoginRequest("admin", "pwd"), CancellationToken.None);

        result.RequiresPasswordChange.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_WhenMustChangePassword_ReturnsNullTokens()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.MarkMustChangePassword();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("pwd", "hash")).Returns(true);
        var service = CreateService(dbContext, hasher: hasher);

        var result = await service.LoginAsync(new LoginRequest("admin", "pwd"), CancellationToken.None);

        result.Tokens.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WhenMustChangePassword_ReturnsNonNullChallengeId()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.MarkMustChangePassword();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("pwd", "hash")).Returns(true);
        var service = CreateService(dbContext, hasher: hasher);

        var result = await service.LoginAsync(new LoginRequest("admin", "pwd"), CancellationToken.None);

        result.PasswordChangeChallengeId.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_WhenMustChangePasswordAndTwoFactorEnabled_DoesNotCreateTwoFactorChallenge()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.MarkMustChangePassword();
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("pwd", "hash")).Returns(true);
        var service = CreateService(dbContext, hasher: hasher);

        var result = await service.LoginAsync(new LoginRequest("admin", "pwd"), CancellationToken.None);

        result.RequiresPasswordChange.Should().BeTrue();
        (await dbContext.TwoFactorChallenges.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task LoginAsync_WhenMustChangePasswordFalse_ReturnsTokensAsNormal()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("pwd", "hash")).Returns(true);
        var service = CreateService(dbContext, hasher: hasher);

        var result = await service.LoginAsync(new LoginRequest("admin", "pwd"), CancellationToken.None);

        result.Tokens.Should().NotBeNull();
        result.RequiresPasswordChange.Should().BeFalse();
    }

    [Fact]
    public async Task ForcedChangePasswordAsync_WithValidChallenge_UpdatesPasswordHash()
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

        await service.ForcedChangePasswordAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        user.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public async Task ForcedChangePasswordAsync_WithValidChallenge_ClearsMustChangePasswordFlag()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.MarkMustChangePassword();
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.ForcedChangePasswordAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        user.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task ForcedChangePasswordAsync_WithValidChallenge_ReturnsTokens()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.MarkMustChangePassword();
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var tokenFactory = new Mock<IJwtTokenFactory>();
        tokenFactory.Setup(x => x.CreateTokens(It.IsAny<User>(), It.IsAny<Dictionary<string, byte[]>>()))
            .Returns(new AuthTokensResponse("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(15)));
        var service = CreateService(dbContext, tokenFactory: tokenFactory);

        var result = await service.ForcedChangePasswordAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ForcedChangePasswordAsync_WithValidChallenge_MarksChallengeAsUsed()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.MarkMustChangePassword();
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.ForcedChangePasswordAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        challenge.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ForcedChangePasswordAsync_WithExpiredChallenge_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(1));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();
        // manually expire via EF change tracker
        dbContext.Entry(challenge).Property("ExpiresAt").CurrentValue = DateTime.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var act = () => service.ForcedChangePasswordAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidPasswordChangeChallenge);
    }

    [Fact]
    public async Task ForcedChangePasswordAsync_WithAlreadyUsedChallenge_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        challenge.MarkAsUsed();
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var act = () => service.ForcedChangePasswordAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidPasswordChangeChallenge);
    }

    [Fact]
    public async Task ForcedChangePasswordAsync_WithNonExistentChallengeId_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var act = () => service.ForcedChangePasswordAsync(new ForcedPasswordChangeRequest(Guid.NewGuid(), "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidPasswordChangeChallenge);
    }

    [Fact]
    public async Task ForcedChangePasswordAsync_WhenUserIsInactive_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = false };
        dbContext.Users.Add(user);
        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var act = () => service.ForcedChangePasswordAsync(new ForcedPasswordChangeRequest(challenge.Id, "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.UserInactive);
    }

    private static AuthService CreateService(
        AuthDbContext dbContext,
        Mock<IPasswordHasher>? hasher = null,
        Mock<IJwtTokenFactory>? tokenFactory = null,
        Mock<ISearchIndexService>? searchIndexService = null)
    {
        var hasCustomHasher = hasher is not null;
        hasher ??= new Mock<IPasswordHasher>();
        if (!hasCustomHasher)
        {
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
            hasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        }

        var hasCustomTokenFactory = tokenFactory is not null;
        tokenFactory ??= new Mock<IJwtTokenFactory>();
        if (!hasCustomTokenFactory)
        {
            tokenFactory.Setup(x => x.CreateTokens(It.IsAny<User>(), It.IsAny<Dictionary<string, byte[]>>()))
                .Returns(new AuthTokensResponse("access", "refresh", DateTime.UtcNow.AddMinutes(15)));
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
                Secret = "super-secret-key-min-32-characters-long!",
                Issuer = "auth-service",
                Audience = "auth-service-clients",
                AccessTokenExpirationMinutes = 15,
                RefreshTokenExpirationDays = 7
            }
        });

        return new AuthService(
            dbContext,
            hasher.Object,
            tokenFactory.Object,
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
