using Auth.Application;
using Auth.Application.Verification;
using Auth.Application.Verification.Commands.SendEmailVerification;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Verification.Commands.SendEmailVerification;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Verification.Commands;

public sealed class SendEmailVerificationCommandHandlerTests
{
    private static SendEmailVerificationCommandHandler CreateHandler(AuthDbContext dbContext)
    {
        var options = Options.Create(new IntegrationOptions
        {
            EncryptionKey = "test-key-12345678",
            TwoFactor = new TwoFactorOptions { StaticOtpForTesting = "123456" }
        });
        return new SendEmailVerificationCommandHandler(
            dbContext, new Mock<IEventBus>().Object, options, new Mock<IAuditContext>().Object,
            new Mock<ILogger<SendEmailVerificationCommandHandler>>().Object);
    }

    [Fact]
    public async Task Send_WhenUserNotFound_Throws()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new SendEmailVerificationCommand(Guid.NewGuid()),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.Code.Should().Be(VerificationErrorCatalog.UserNotFound);
    }

    [Fact]
    public async Task Send_WhenNoEmail_Throws()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "noemail",
            Email = "",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new SendEmailVerificationCommand(user.Id),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.Code.Should().Be(VerificationErrorCatalog.NoEmailConfigured);
    }

    [Fact]
    public async Task Send_WhenActiveChallenge_Throws()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "cooldown",
            Email = "cooldown@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.Users.Add(user);

        var salt = TwoFactorOtpSecurity.CreateSalt();
        var hash = TwoFactorOtpSecurity.HashOtp("123456", salt);
        var encrypted = TwoFactorOtpSecurity.EncryptOtp("123456", "test-key-12345678");
        var existingChallenge = TwoFactorChallenge.Create(
            user.Id,
            TwoFactorChallenge.PurposeEmailVerification,
            TwoFactorChannel.Email,
            hash, salt, encrypted,
            DateTime.UtcNow.AddMinutes(5),
            5);
        dbContext.TwoFactorChallenges.Add(existingChallenge);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new SendEmailVerificationCommand(user.Id),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.Code.Should().Be(VerificationErrorCatalog.VerificationCooldown);
    }

    [Fact]
    public async Task Send_WhenValid_CreatesChallenge()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "valid",
            Email = "valid@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new SendEmailVerificationCommand(user.Id),
            CancellationToken.None);

        result.ChallengeId.Should().NotBeEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        var challenge = await dbContext.TwoFactorChallenges.FindAsync(result.ChallengeId);
        challenge.Should().NotBeNull();
        challenge!.UserId.Should().Be(user.Id);
        challenge.Purpose.Should().Be(TwoFactorChallenge.PurposeEmailVerification);
        challenge.Channel.Should().Be(TwoFactorChannel.Email);
    }
}
