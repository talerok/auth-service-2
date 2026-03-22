using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Authentication.Commands.CreateLoginChallenge;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Auth.UnitTests.Auth.Commands;

public sealed class CreateLoginChallengeCommandHandlerTests
{
    [Fact]
    public async Task Handle_EmailChannel_CreatesChallenge()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new CreateLoginChallengeCommand(userId, TwoFactorChannel.Email),
            CancellationToken.None);

        result.UserId.Should().Be(userId);
        result.Channel.Should().Be(TwoFactorChannel.Email);
        result.Purpose.Should().Be(TwoFactorChallenge.PurposeLogin);
        result.IsUsed.Should().BeFalse();

        var persisted = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == result.Id);
        persisted.Channel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task Handle_UnsupportedChannel_Throws()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new CreateLoginChallengeCommand(userId, (TwoFactorChannel)99),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == TwoFactorErrorCatalog.UnsupportedChannel);
    }

    private static CreateLoginChallengeCommandHandler CreateHandler(AuthDbContext dbContext) =>
        new(dbContext, CreateOptions(), NullLogger<CreateLoginChallengeCommandHandler>.Instance);

    private static IOptions<IntegrationOptions> CreateOptions() =>
        Options.Create(new IntegrationOptions
        {
            TwoFactor = new TwoFactorOptions { EncryptionKey = "super-secret-key-min-32-characters-long!", StaticOtpForTesting = "123456" }
        });

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
