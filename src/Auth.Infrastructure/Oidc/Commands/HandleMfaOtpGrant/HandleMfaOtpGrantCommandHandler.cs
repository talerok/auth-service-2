using System.Security.Claims;
using Auth.Application;
using Auth.Application.Oidc.Commands.HandleMfaOtpGrant;
using Auth.Application.TwoFactor.Commands.ValidateLoginOtp;
using Auth.Domain;
using MediatR;

namespace Auth.Infrastructure.Oidc.Commands.HandleMfaOtpGrant;

internal sealed class HandleMfaOtpGrantCommandHandler(
    ISender sender) : IRequestHandler<HandleMfaOtpGrantCommand, ClaimsPrincipal>
{
    public async Task<ClaimsPrincipal> Handle(HandleMfaOtpGrantCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.MfaToken)
            || string.IsNullOrWhiteSpace(command.Otp)
            || string.IsNullOrWhiteSpace(command.MfaChannel))
            throw new AuthException(AuthErrorCatalog.InvalidRequest);

        if (!Guid.TryParse(command.MfaToken, out var challengeId)
            || !Enum.TryParse<TwoFactorChannel>(command.MfaChannel, true, out var channel))
            throw new AuthException(AuthErrorCatalog.InvalidRequest);

        var user = await sender.Send(new ValidateLoginOtpCommand(challengeId, channel, command.Otp), cancellationToken);
        return await OidcPrincipalFactory.CreateUserPrincipalAsync(
            user, command.Scopes, sender, command.ClientId, cancellationToken);
    }
}
