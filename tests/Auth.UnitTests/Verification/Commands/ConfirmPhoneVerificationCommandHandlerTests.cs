using Auth.Application;
using Auth.Application.Verification;
using Auth.Application.Verification.Commands.ConfirmPhoneVerification;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Verification.Commands.ConfirmPhoneVerification;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Verification.Commands;

public sealed class ConfirmPhoneVerificationCommandHandlerTests
{
    private static ConfirmPhoneVerificationCommandHandler CreateHandler(AuthDbContext dbContext)
    {
        return new ConfirmPhoneVerificationCommandHandler(
            dbContext,
            new Mock<IAuditContext>().Object,
            new Mock<ILogger<ConfirmPhoneVerificationCommandHandler>>().Object);
    }

    [Fact]
    public async Task Confirm_WhenChallengeNotFound_Throws()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new ConfirmPhoneVerificationCommand(Guid.NewGuid(), "123456"),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.Code.Should().Be(VerificationErrorCatalog.InvalidChallenge);
    }

    [Fact]
    public async Task Confirm_WhenExpired_Throws()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "expired",
            Email = "expired@example.com",
            Phone = "+71234567890",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.Users.Add(user);

        var otp = "123456";
        var salt = TwoFactorOtpSecurity.CreateSalt();
        var hash = TwoFactorOtpSecurity.HashOtp(otp, salt);
        var encrypted = TwoFactorOtpSecurity.EncryptOtp(otp, "test-key");
        var challenge = TwoFactorChallenge.Create(
            user.Id,
            TwoFactorChallenge.PurposePhoneVerification,
            TwoFactorChannel.Sms,
            hash, salt, encrypted,
            DateTime.UtcNow.AddMinutes(-1),
            5);
        challenge.MarkDelivered();
        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new ConfirmPhoneVerificationCommand(challenge.Id, otp),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.Code.Should().Be(VerificationErrorCatalog.ChallengeExpired);
    }

    [Fact]
    public async Task Confirm_WhenInvalidOtp_IncrementsAttemptsAndThrows()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "badotp",
            Email = "badotp@example.com",
            Phone = "+71234567890",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.Users.Add(user);

        var otp = "123456";
        var salt = TwoFactorOtpSecurity.CreateSalt();
        var hash = TwoFactorOtpSecurity.HashOtp(otp, salt);
        var encrypted = TwoFactorOtpSecurity.EncryptOtp(otp, "test-key");
        var challenge = TwoFactorChallenge.Create(
            user.Id,
            TwoFactorChallenge.PurposePhoneVerification,
            TwoFactorChannel.Sms,
            hash, salt, encrypted,
            DateTime.UtcNow.AddMinutes(5),
            5);
        challenge.MarkDelivered();
        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new ConfirmPhoneVerificationCommand(challenge.Id, "000000"),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.Code.Should().Be(VerificationErrorCatalog.InvalidOtp);

        var updated = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == challenge.Id);
        updated.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task Confirm_WhenValid_SetsPhoneVerified()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "verified",
            Email = "verified@example.com",
            Phone = "+71234567890",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.Users.Add(user);

        var otp = "123456";
        var salt = TwoFactorOtpSecurity.CreateSalt();
        var hash = TwoFactorOtpSecurity.HashOtp(otp, salt);
        var encrypted = TwoFactorOtpSecurity.EncryptOtp(otp, "test-key");
        var challenge = TwoFactorChallenge.Create(
            user.Id,
            TwoFactorChallenge.PurposePhoneVerification,
            TwoFactorChannel.Sms,
            hash, salt, encrypted,
            DateTime.UtcNow.AddMinutes(5),
            5);
        challenge.MarkDelivered();
        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext);

        await handler.Handle(
            new ConfirmPhoneVerificationCommand(challenge.Id, otp),
            CancellationToken.None);

        var updatedUser = await dbContext.Users.SingleAsync(x => x.Id == user.Id);
        updatedUser.PhoneVerified.Should().BeTrue();

        var updatedChallenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == challenge.Id);
        updatedChallenge.IsUsed.Should().BeTrue();
        updatedChallenge.CompletedAt.Should().NotBeNull();
    }
}
