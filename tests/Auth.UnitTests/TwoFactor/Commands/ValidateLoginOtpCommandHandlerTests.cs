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

        var handler = new ValidateLoginOtpCommandHandler(dbContext, NullLogger<ValidateLoginOtpCommandHandler>.Instance);
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

        var handler = new ValidateLoginOtpCommandHandler(dbContext, NullLogger<ValidateLoginOtpCommandHandler>.Instance);
        await handler.Handle(
            new ValidateLoginOtpCommand(loginChallenge.Id, TwoFactorChannel.Email, "123456"),
            CancellationToken.None);

        var second = () => handler.Handle(
            new ValidateLoginOtpCommand(loginChallenge.Id, TwoFactorChannel.Email, "123456"),
            CancellationToken.None);
        await second.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == TwoFactorErrorCatalog.OtpAlreadyUsed);
    }

    private static CreateLoginChallengeCommandHandler CreateChallengeHandler(AuthDbContext dbContext, string twoFactorStaticOtp) =>
        new(dbContext,
            Options.Create(new IntegrationOptions
            {
                TwoFactor = new TwoFactorOptions { EncryptionKey = "super-secret-key-min-32-characters-long!", StaticOtpForTesting = twoFactorStaticOtp, DeliveryPollIntervalMilliseconds = 5 }
            }),
            NullLogger<CreateLoginChallengeCommandHandler>.Instance);

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
