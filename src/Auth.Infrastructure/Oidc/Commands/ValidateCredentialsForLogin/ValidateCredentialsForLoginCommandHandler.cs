using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Application.Auth.Commands.CreatePasswordChangeChallenge;
using Auth.Application.Auth.Commands.ValidateCredentials;
using Auth.Application.Oidc.Commands.ValidateCredentialsForLogin;
using MediatR;

namespace Auth.Infrastructure.Oidc.Commands.ValidateCredentialsForLogin;

internal sealed class ValidateCredentialsForLoginCommandHandler(
    ISender sender) : IRequestHandler<ValidateCredentialsForLoginCommand, CredentialValidationResult>
{
    public async Task<CredentialValidationResult> Handle(ValidateCredentialsForLoginCommand command, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new ValidateCredentialsCommand(command.Username, command.Password), cancellationToken);

        if (user.MustChangePassword)
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

        var principal = await OidcPrincipalFactory.CreateUserPrincipalAsync(
            user, command.Scopes, sender, command.ClientId, cancellationToken);
        return new CredentialValidationResult.Success(principal);
    }
}
