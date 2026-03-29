using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Application.TwoFactor.Commands.ValidateLoginOtp;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Authentication.Commands.CreateLoginChallenge;
using Auth.Infrastructure.TwoFactor.Commands.ValidateLoginOtp;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.TwoFactor.Commands;

public sealed class ValidateLoginOtpCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenAttemptsExceedLimit_ThrowsAttemptsExceeded()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var challengeHandler = CreateChallengeHandler(dbContext, "123456");
        var loginChallenge = await challengeHandler.Handle(new CreateLoginChallengeCommand(user.Id, TwoFactorChannel.Email), CancellationToken.None);
        var challenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == loginChallenge.Id);
        challenge.MarkDelivered();
        await dbContext.SaveChangesAsync();

        var handler = new ValidateLoginOtpCommandHandler(dbContext, new Mock<IAuditContext>().Object, NullLogger<ValidateLoginOtpCommandHandler>.Instance);
        for (var i = 0; i < 5; i++)
        {
            var act = () => handler.Handle(
                new ValidateLoginOtpCommand(loginChallenge.Id, TwoFactorChannel.Email, "000000"),
                CancellationToken.None);
            await act.Should().ThrowAsync<AuthException>()
                .Where(x => x.Code == TwoFactorErrorCatalog.VerificationFailed);
        }

        var blocked = () => handler.Handle(
            new ValidateLoginOtpCommand(loginChallenge.Id, TwoFactorChannel.Email, "123456"),
            CancellationToken.None);
        await blocked.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == TwoFactorErrorCatalog.AttemptsExceeded);
    }

    [Fact]
    public async Task Handle_WhenOtpAlreadyUsed_RejectsSecondAttempt()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var challengeHandler = CreateChallengeHandler(dbContext, "123456");
        var loginChallenge = await challengeHandler.Handle(new CreateLoginChallengeCommand(user.Id, TwoFactorChannel.Email), CancellationToken.None);
        var challenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == loginChallenge.Id);
        challenge.MarkDelivered();
        await dbContext.SaveChangesAsync();

        var handler = new ValidateLoginOtpCommandHandler(dbContext, new Mock<IAuditContext>().Object, NullLogger<ValidateLoginOtpCommandHandler>.Instance);
        await handler.Handle(
            new ValidateLoginOtpCommand(loginChallenge.Id, TwoFactorChannel.Email, "123456"),
            CancellationToken.None);

        var second = () => handler.Handle(
            new ValidateLoginOtpCommand(loginChallenge.Id, TwoFactorChannel.Email, "123456"),
            CancellationToken.None);
        await second.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == TwoFactorErrorCatalog.OtpAlreadyUsed);
    }

    [Fact]
    public async Task Handle_InvalidOtp_SetsFailureAuditDetails()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var challengeHandler = CreateChallengeHandler(dbContext, "123456");
        var loginChallenge = await challengeHandler.Handle(new CreateLoginChallengeCommand(user.Id, TwoFactorChannel.Email), CancellationToken.None);
        var challenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == loginChallenge.Id);
        challenge.MarkDelivered();
        await dbContext.SaveChangesAsync();

        var auditContext = new Mock<IAuditContext>();
        auditContext.SetupAllProperties();
        var handler = new ValidateLoginOtpCommandHandler(dbContext, auditContext.Object, NullLogger<ValidateLoginOtpCommandHandler>.Instance);

        var act = () => handler.Handle(
            new ValidateLoginOtpCommand(loginChallenge.Id, TwoFactorChannel.Email, "000000"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>();
        auditContext.Object.Details.Should().NotBeNull();
        auditContext.Object.Details!["channel"].Should().Be("Email");
        auditContext.Object.Details!["result"].Should().Be("failure");
    }

    [Fact]
    public async Task Handle_ValidOtp_SetsSuccessAuditDetails()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var challengeHandler = CreateChallengeHandler(dbContext, "123456");
        var loginChallenge = await challengeHandler.Handle(new CreateLoginChallengeCommand(user.Id, TwoFactorChannel.Email), CancellationToken.None);
        var challenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == loginChallenge.Id);
        challenge.MarkDelivered();
        await dbContext.SaveChangesAsync();

        var auditContext = new Mock<IAuditContext>();
        auditContext.SetupAllProperties();
        var handler = new ValidateLoginOtpCommandHandler(dbContext, auditContext.Object, NullLogger<ValidateLoginOtpCommandHandler>.Instance);

        await handler.Handle(
            new ValidateLoginOtpCommand(loginChallenge.Id, TwoFactorChannel.Email, "123456"),
            CancellationToken.None);

        auditContext.Object.Details.Should().NotBeNull();
        auditContext.Object.Details!["channel"].Should().Be("Email");
        auditContext.Object.Details!["result"].Should().Be("success");
        auditContext.Object.EntityId.Should().Be(user.Id);
    }

    private static CreateLoginChallengeCommandHandler CreateChallengeHandler(AuthDbContext dbContext, string twoFactorStaticOtp) =>
        new(dbContext,
            new Moq.Mock<IEventBus>().Object,
            Options.Create(new IntegrationOptions { EncryptionKey = "super-secret-key-min-32-characters-long!", TwoFactor = new TwoFactorOptions { StaticOtpForTesting = twoFactorStaticOtp, DeliveryPollIntervalMilliseconds = 5 }
            }),
            NullLogger<CreateLoginChallengeCommandHandler>.Instance);

}
