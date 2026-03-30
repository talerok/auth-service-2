using System.Security.Claims;
using Auth.Application;
using Auth.Application.Oidc.Commands.HandleMfaOtpGrant;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using Auth.Application.Sessions.Commands.CreateSession;
using Auth.Application.TwoFactor.Commands.ValidateLoginOtp;
using Auth.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Auth.Infrastructure.Oidc.Commands.HandleMfaOtpGrant;

internal sealed class HandleMfaOtpGrantCommandHandler(
    ISender sender,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<HandleMfaOtpGrantCommand, ClaimsPrincipal>
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
        var (ip, ua) = httpContextAccessor.GetClientInfo();
        var sessionId = await sender.Send(
            new CreateSessionCommand(user.Id, command.ClientId, "pwd+otp", ip, ua), cancellationToken);
        return await sender.Send(
            new BuildPrincipalQuery(user.Id, command.Scopes, command.ClientId, ["pwd", "otp"], sessionId), cancellationToken);
    }
}
