using Auth.Application;
using Auth.Application.TwoFactor.Commands.EnableTwoFactor;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.TwoFactor.Commands.EnableTwoFactor;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.TwoFactor.Commands;

public sealed class EnableTwoFactorCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmailChannel_CreatesChallenge()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new EnableTwoFactorCommand(user.Id, TwoFactorChannel.Email),
            CancellationToken.None);

        result.Channel.Should().Be(TwoFactorChannel.Email);
        var challenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == result.ChallengeId);
        challenge.Channel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task Handle_WhenSmsChannelAndPhoneSet_CreatesChallenge()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "smsuser", Email = "sms@example.com", Phone = "+71234567890", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new EnableTwoFactorCommand(user.Id, TwoFactorChannel.Sms),
            CancellationToken.None);

        result.Channel.Should().Be(TwoFactorChannel.Sms);
    }

    [Fact]
    public async Task Handle_WhenSmsChannelAndNoPhone_ThrowsPhoneRequired()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "nophone", Email = "nophone@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new EnableTwoFactorCommand(user.Id, TwoFactorChannel.Sms),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == TwoFactorErrorCatalog.PhoneRequired);
    }

    [Fact]
    public async Task Handle_DoesNotStorePlainTextOtp()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new EnableTwoFactorCommand(user.Id, TwoFactorChannel.Email),
            CancellationToken.None);

        var challenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == result.ChallengeId);
        challenge.OtpHash.Should().NotBe("123456");
        challenge.OtpSalt.Should().NotBeNullOrWhiteSpace();
        challenge.OtpEncrypted.Should().NotBeNullOrWhiteSpace();
    }

    private static EnableTwoFactorCommandHandler CreateHandler(AuthDbContext dbContext) =>
        new(dbContext, CreateOptions(), new Mock<IAuditContext>().Object, NullLogger<EnableTwoFactorCommandHandler>.Instance);

    private static IOptions<IntegrationOptions> CreateOptions() =>
        Options.Create(new IntegrationOptions { EncryptionKey = "super-secret-key-min-32-characters-long!", TwoFactor = new TwoFactorOptions { StaticOtpForTesting = "123456", DeliveryPollIntervalMilliseconds = 5 }
        });

}
