using Auth.Application;
using Auth.Application.Auth.Commands.CreatePasswordChangeChallenge;
using Auth.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Authentication.Commands.CreatePasswordChangeChallenge;

internal sealed class CreatePasswordChangeChallengeCommandHandler(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options,
    ILogger<CreatePasswordChangeChallengeCommandHandler> logger) : IRequestHandler<CreatePasswordChangeChallengeCommand, PasswordChangeChallenge>
{
    private readonly PasswordChangeOptions _passwordChange = options.Value.PasswordChange;

    public async Task<PasswordChangeChallenge> Handle(CreatePasswordChangeChallengeCommand command, CancellationToken cancellationToken)
    {
        var challenge = PasswordChangeChallenge.Create(
            command.UserId,
            DateTime.UtcNow.AddMinutes(_passwordChange.PasswordChangeTtlMinutes));

        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "PasswordChangeOperation userId={UserId} operation={Operation} result={Result}",
            command.UserId, "CHALLENGE_CREATED", "SUCCESS");

        return challenge;
    }
}
