using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Application.Auth.Commands.CreatePasswordChangeChallenge;
using Auth.Application.Auth.Commands.ValidateCredentials;
using Auth.Application.Oidc.Commands.ValidateCredentialsForLogin;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using MediatR;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Oidc.Commands.ValidateCredentialsForLogin;

internal sealed class ValidateCredentialsForLoginCommandHandler(
    ISender sender,
    IOptions<PasswordExpirationOptions> passwordExpirationOptions) : IRequestHandler<ValidateCredentialsForLoginCommand, CredentialValidationResult>
{
    public async Task<CredentialValidationResult> Handle(ValidateCredentialsForLoginCommand command, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new ValidateCredentialsCommand(command.Username, command.Password), cancellationToken);

        if (user.MustChangePassword)
        {
            var challenge = await sender.Send(new CreatePasswordChangeChallengeCommand(user.Id), cancellationToken);
            return new CredentialValidationResult.PasswordChangeRequired(challenge.Id);
        }

        if (user.IsPasswordExpired(passwordExpirationOptions.Value.DefaultMaxAgeDays))
        {
            var challenge = await sender.Send(new CreatePasswordChangeChallengeCommand(user.Id), cancellationToken);
            return new CredentialValidationResult.PasswordChangeRequired(challenge.Id);
        }

        if (user.TwoFactorEnabled)
        {
            var mfaChallenge = await sender.Send(
                new CreateLoginChallengeCommand(user.Id, user.TwoFactorChannel!.Value), cancellationToken);
            return new CredentialValidationResult.MfaRequired(mfaChallenge.Id, mfaChallenge.Channel);
        }

        var principal = await sender.Send(
            new BuildPrincipalQuery(user.Id, command.Scopes, command.ClientId, ["pwd"]), cancellationToken);
        return new CredentialValidationResult.Success(principal);
    }
}
