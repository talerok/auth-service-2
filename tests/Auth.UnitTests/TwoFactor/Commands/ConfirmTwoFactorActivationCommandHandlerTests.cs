using Auth.Application;
using Auth.Application.TwoFactor.Commands.ConfirmTwoFactorActivation;
using Auth.Application.TwoFactor.Commands.EnableTwoFactor;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.TwoFactor.Commands.ConfirmTwoFactorActivation;
using Auth.Infrastructure.TwoFactor.Commands.EnableTwoFactor;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Auth.UnitTests.TwoFactor.Commands;

public sealed class ConfirmTwoFactorActivationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenOtpIsValid_EnablesTwoFactor()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var enableHandler = new EnableTwoFactorCommandHandler(dbContext, CreateOptions(), NullLogger<EnableTwoFactorCommandHandler>.Instance);
        var started = await enableHandler.Handle(new EnableTwoFactorCommand(user.Id, TwoFactorChannel.Email), CancellationToken.None);
        var challenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == started.ChallengeId);
        challenge.MarkDelivered();
        await dbContext.SaveChangesAsync();

        var handler = new ConfirmTwoFactorActivationCommandHandler(dbContext, NullLogger<ConfirmTwoFactorActivationCommandHandler>.Instance);
        await handler.Handle(
            new ConfirmTwoFactorActivationCommand(user.Id, started.ChallengeId, TwoFactorChannel.Email, "123456"),
            CancellationToken.None);

        var updatedUser = await dbContext.Users.SingleAsync(x => x.Id == user.Id);
        updatedUser.TwoFactorEnabled.Should().BeTrue();
        updatedUser.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task Handle_WithSmsChannel_EnablesTwoFactorSms()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "sms", Email = "sms@example.com", Phone = "+71234567890", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var enableHandler = new EnableTwoFactorCommandHandler(dbContext, CreateOptions(), NullLogger<EnableTwoFactorCommandHandler>.Instance);
        var started = await enableHandler.Handle(new EnableTwoFactorCommand(user.Id, TwoFactorChannel.Sms), CancellationToken.None);
        var challenge = await dbContext.TwoFactorChallenges.SingleAsync(x => x.Id == started.ChallengeId);
        challenge.MarkDelivered();
        await dbContext.SaveChangesAsync();

        var handler = new ConfirmTwoFactorActivationCommandHandler(dbContext, NullLogger<ConfirmTwoFactorActivationCommandHandler>.Instance);
        await handler.Handle(
            new ConfirmTwoFactorActivationCommand(user.Id, started.ChallengeId, TwoFactorChannel.Sms, "123456"),
            CancellationToken.None);

        var updatedUser = await dbContext.Users.SingleAsync(x => x.Id == user.Id);
        updatedUser.TwoFactorEnabled.Should().BeTrue();
        updatedUser.TwoFactorChannel.Should().Be(TwoFactorChannel.Sms);
    }

    private static IOptions<IntegrationOptions> CreateOptions() =>
        Options.Create(new IntegrationOptions
        {
            Jwt = new JwtOptions { Secret = "super-secret-key-min-32-characters-long!" },
            TwoFactor = new TwoFactorOptions { StaticOtpForTesting = "123456", DeliveryPollIntervalMilliseconds = 5 }
        });

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
