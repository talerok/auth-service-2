using Auth.Application.Auth.Commands.CreatePasswordChangeChallenge;
using Auth.Infrastructure;
using Auth.Infrastructure.Authentication.Commands.CreatePasswordChangeChallenge;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Auth.Commands;

public sealed class CreatePasswordChangeChallengeCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesAndPersistsChallenge()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var handler = new CreatePasswordChangeChallengeCommandHandler(
            dbContext, CreateOptions(), NullLogger<CreatePasswordChangeChallengeCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreatePasswordChangeChallengeCommand(userId),
            CancellationToken.None);

        result.UserId.Should().Be(userId);
        result.IsUsed.Should().BeFalse();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        var persisted = await dbContext.PasswordChangeChallenges.FirstAsync(x => x.Id == result.Id);
        persisted.UserId.Should().Be(userId);
    }

    private static IOptions<IntegrationOptions> CreateOptions() =>
        Options.Create(new IntegrationOptions
        {
            PasswordChange = new PasswordChangeOptions { PasswordChangeTtlMinutes = 15 }
        });

}
