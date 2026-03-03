using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Application.Auth.Commands.CreatePasswordChangeChallenge;
using Auth.Application.Auth.Commands.ValidateCredentials;
using Auth.Application.Oidc.Commands.HandlePasswordGrant;
using MediatR;

namespace Auth.Infrastructure.Oidc.Commands.HandlePasswordGrant;

internal sealed class HandlePasswordGrantCommandHandler(
    ISender sender) : IRequestHandler<HandlePasswordGrantCommand, PasswordGrantResult>
{
    public async Task<PasswordGrantResult> Handle(HandlePasswordGrantCommand command, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new ValidateCredentialsCommand(command.Username, command.Password), cancellationToken);

        if (user.MustChangePassword)
        {
            var challenge = await sender.Send(new CreatePasswordChangeChallengeCommand(user.Id), cancellationToken);
            return new PasswordGrantResult.PasswordChangeRequired(challenge.Id);
        }

        if (user.TwoFactorEnabled)
        {
            var mfaChallenge = await sender.Send(
                new CreateLoginChallengeCommand(user.Id, user.TwoFactorChannel!.Value), cancellationToken);
            return new PasswordGrantResult.MfaRequired(mfaChallenge.Id, mfaChallenge.Channel);
        }

        var principal = await OidcPrincipalFactory.CreateUserPrincipalAsync(user, command.Scopes, sender, cancellationToken);
        return new PasswordGrantResult.Success(principal);
    }
}
