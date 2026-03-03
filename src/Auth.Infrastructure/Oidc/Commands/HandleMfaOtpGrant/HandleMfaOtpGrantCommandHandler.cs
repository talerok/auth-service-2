using System.Security.Claims;
using Auth.Application.Oidc.Commands.HandleMfaOtpGrant;
using Auth.Application.TwoFactor.Commands.ValidateLoginOtp;
using MediatR;

namespace Auth.Infrastructure.Oidc.Commands.HandleMfaOtpGrant;

internal sealed class HandleMfaOtpGrantCommandHandler(
    ISender sender) : IRequestHandler<HandleMfaOtpGrantCommand, ClaimsPrincipal>
{
    public async Task<ClaimsPrincipal> Handle(HandleMfaOtpGrantCommand command, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new ValidateLoginOtpCommand(command.ChallengeId, command.Channel, command.Otp), cancellationToken);
        return await OidcPrincipalFactory.CreateUserPrincipalAsync(user, command.Scopes, sender, cancellationToken);
    }
}
