using System.IdentityModel.Tokens.Jwt;
using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Application.Oidc.Commands.HandleJwtBearerGrant;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Oidc.Commands.HandleJwtBearerGrant;

internal sealed class HandleJwtBearerGrantCommandHandler(
    AuthDbContext dbContext,
    ISender sender,
    IOidcTokenValidator tokenValidator) : IRequestHandler<HandleJwtBearerGrantCommand, CredentialValidationResult>
{
    public async Task<CredentialValidationResult> Handle(HandleJwtBearerGrantCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Assertion))
            throw new AuthException(AuthErrorCatalog.InvalidRequest);

        var issuer = ReadIssuerFromJwt(command.Assertion);
        if (string.IsNullOrWhiteSpace(issuer))
            throw new AuthException(AuthErrorCatalog.IdentitySourceTokenInvalid);

        var source = await dbContext.IdentitySources
            .Include(x => x.OidcConfig)
            .FirstOrDefaultAsync(x =>
                x.Type == IdentitySourceType.Oidc &&
                x.OidcConfig != null &&
                x.OidcConfig.Authority == issuer, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        if (!source.IsEnabled)
            throw new AuthException(AuthErrorCatalog.IdentitySourceDisabled);

        var externalIdentity = await tokenValidator.ValidateAndGetSubjectAsync(
            source.OidcConfig!.Authority, source.OidcConfig.ClientId, command.Assertion, cancellationToken);

        var link = await dbContext.IdentitySourceLinks
            .FirstOrDefaultAsync(x => x.IdentitySourceId == source.Id && x.ExternalIdentity == externalIdentity, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceLinkNotFound);

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == link.UserId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceLinkNotFound);

        if (!user.IsActive)
            throw new AuthException(AuthErrorCatalog.IdentitySourceUserInactive);

        if (user.TwoFactorEnabled)
        {
            var mfaChallenge = await sender.Send(
                new CreateLoginChallengeCommand(user.Id, user.TwoFactorChannel!.Value), cancellationToken);
            return new CredentialValidationResult.MfaRequired(mfaChallenge.Id, mfaChallenge.Channel);
        }

        var principal = await sender.Send(new BuildPrincipalQuery(user.Id, command.Scopes, command.ClientId), cancellationToken);
        return new CredentialValidationResult.Success(principal);
    }

    private static string? ReadIssuerFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.Issuer;
        }
        catch
        {
            return null;
        }
    }
}
