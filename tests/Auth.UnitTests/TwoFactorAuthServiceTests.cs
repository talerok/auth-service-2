using Auth.Application;
using Auth.Domain;
using Auth.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Auth.UnitTests;

public sealed class TwoFactorAuthServiceTests
{
    [Fact]
    public async Task EnableAndConfirmTwoFactorAsync_WhenOtpIsValid_EnablesTwoFactor()
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

        var service = CreateService(dbContext, twoFactorStaticOtp: "123456");

        var started = await service.EnableTwoFactorAsync(
            user.Id,
            new EnableTwoFactorRequest(TwoFactorChannel.Email),
            CancellationToken.None);
        var challenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == started.ChallengeId);
        challenge.MarkDelivered();
        await dbContext.SaveChangesAsync();

        await service.ConfirmTwoFactorActivationAsync(
            user.Id,
            new VerifyTwoFactorRequest(started.ChallengeId, TwoFactorChannel.Email, "123456"),
            CancellationToken.None);

        var updatedUser = await dbContext.Users.SingleAsync(x => x.Id == user.Id);
        updatedUser.TwoFactorEnabled.Should().BeTrue();
        updatedUser.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task VerifyTwoFactorLoginAsync_WhenAttemptsExceedLimit_ReturnsDeterministicErrorCode()
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

        var authService = CreateAuthService(dbContext, "123456");
        var login = await authService.LoginAsync(new LoginRequest("admin", "pwd"), CancellationToken.None);
        var challenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == login.ChallengeId);
        challenge.MarkDelivered();
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, twoFactorStaticOtp: "123456");
        for (var i = 0; i < 5; i++)
        {
            var act = () => service.VerifyTwoFactorLoginAsync(
                new VerifyTwoFactorRequest(login.ChallengeId!.Value, TwoFactorChannel.Email, "000000"),
                CancellationToken.None);
            await act.Should().ThrowAsync<AuthException>()
                .Where(x => x.Code == TwoFactorErrorCatalog.VerificationFailed);
        }

        var blocked = () => service.VerifyTwoFactorLoginAsync(
            new VerifyTwoFactorRequest(login.ChallengeId!.Value, TwoFactorChannel.Email, "123456"),
            CancellationToken.None);
        await blocked.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == TwoFactorErrorCatalog.AttemptsExceeded);
    }

    [Fact]
    public async Task VerifyTwoFactorLoginAsync_WhenOtpAlreadyUsed_RejectsSecondAttempt()
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

        var authService = CreateAuthService(dbContext, "123456");
        var login = await authService.LoginAsync(new LoginRequest("admin", "pwd"), CancellationToken.None);
        var challenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == login.ChallengeId);
        challenge.MarkDelivered();
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, twoFactorStaticOtp: "123456");
        await service.VerifyTwoFactorLoginAsync(
            new VerifyTwoFactorRequest(login.ChallengeId!.Value, TwoFactorChannel.Email, "123456"),
            CancellationToken.None);

        var second = () => service.VerifyTwoFactorLoginAsync(
            new VerifyTwoFactorRequest(login.ChallengeId.Value, TwoFactorChannel.Email, "123456"),
            CancellationToken.None);
        await second.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == TwoFactorErrorCatalog.OtpAlreadyUsed);
    }

    [Fact]
    public async Task EnableTwoFactorAsync_WhenChallengeCreated_DoesNotStorePlainTextOtp()
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

        var service = CreateService(dbContext, twoFactorStaticOtp: "123456");
        var started = await service.EnableTwoFactorAsync(
            user.Id,
            new EnableTwoFactorRequest(TwoFactorChannel.Email),
            CancellationToken.None);
        var challenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == started.ChallengeId);

        challenge.OtpHash.Should().NotBe("123456");
        challenge.OtpSalt.Should().NotBeNullOrWhiteSpace();
        challenge.OtpEncrypted.Should().NotBeNullOrWhiteSpace();
    }

    private static TwoFactorAuthService CreateService(AuthDbContext dbContext, string twoFactorStaticOtp)
    {
        var tokenFactory = new Mock<IJwtTokenFactory>();
        tokenFactory
            .Setup(x => x.CreateTokens(It.IsAny<User>(), It.IsAny<Dictionary<Guid, byte[]>>()))
            .Returns(new AuthTokensResponse("access", "refresh", DateTime.UtcNow.AddMinutes(15)));
        var options = CreateOptions(twoFactorStaticOtp);

        return new TwoFactorAuthService(
            dbContext,
            tokenFactory.Object,
            options,
            NullLogger<TwoFactorAuthService>.Instance);
    }

    private static AuthService CreateAuthService(AuthDbContext dbContext, string twoFactorStaticOtp)
    {
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Verify("pwd", "hash")).Returns(true);

        var tokenFactory = new Mock<IJwtTokenFactory>();
        tokenFactory
            .Setup(x => x.CreateTokens(It.IsAny<User>(), It.IsAny<Dictionary<Guid, byte[]>>()))
            .Returns(new AuthTokensResponse("access", "refresh", DateTime.UtcNow.AddMinutes(15)));

        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexUserAsync(It.IsAny<UserDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new AuthService(
            dbContext,
            hasher.Object,
            tokenFactory.Object,
            searchIndex.Object,
            CreateOptions(twoFactorStaticOtp),
            NullLogger<AuthService>.Instance);
    }

    private static IOptions<IntegrationOptions> CreateOptions(string twoFactorStaticOtp) =>
        Options.Create(new IntegrationOptions
        {
            Jwt = new JwtOptions
            {
                Secret = "super-secret-key-min-32-characters-long!",
                Issuer = "auth-service",
                Audience = "auth-service-clients",
                AccessTokenExpirationMinutes = 15,
                RefreshTokenExpirationDays = 7
            },
            TwoFactor = new TwoFactorOptions
            {
                StaticOtpForTesting = twoFactorStaticOtp,
                DeliveryPollIntervalMilliseconds = 5
            }
        });

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuthDbContext(options);
    }
}
