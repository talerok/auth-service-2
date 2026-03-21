using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Application.Oidc.Commands.AuthenticateViaIdentitySource;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Oidc.Commands.AuthenticateViaIdentitySource;

internal sealed class AuthenticateViaIdentitySourceCommandHandler(
    AuthDbContext dbContext,
    ISender sender,
    IOidcTokenValidator tokenValidator,
    ILdapAuthenticator ldapAuthenticator) : IRequestHandler<AuthenticateViaIdentitySourceCommand, CredentialValidationResult>
{
    public async Task<CredentialValidationResult> Handle(AuthenticateViaIdentitySourceCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.IdentitySourceName) || string.IsNullOrWhiteSpace(command.Token))
            throw new AuthException(AuthErrorCatalog.InvalidRequest);

        var source = await dbContext.IdentitySources
            .Include(x => x.OidcConfig)
            .Include(x => x.LdapConfig)
            .FirstOrDefaultAsync(x => x.Name == command.IdentitySourceName, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        if (!source.IsEnabled)
            throw new AuthException(AuthErrorCatalog.IdentitySourceDisabled);

        string externalIdentity;

        switch (source.Type)
        {
            case IdentitySourceType.Oidc:
                if (source.OidcConfig is null)
                    throw new AuthException(AuthErrorCatalog.IdentitySourceTypeMismatch);

                externalIdentity = await tokenValidator.ValidateAndGetSubjectAsync(
                    source.OidcConfig.Authority, source.OidcConfig.ClientId, command.Token, cancellationToken);
                break;

            case IdentitySourceType.Ldap:
                if (source.LdapConfig is null)
                    throw new AuthException(AuthErrorCatalog.IdentitySourceTypeMismatch);

                if (string.IsNullOrWhiteSpace(command.Username))
                    throw new AuthException(AuthErrorCatalog.IdentitySourceUsernameRequired);

                await ldapAuthenticator.AuthenticateAsync(source.LdapConfig, command.Username, command.Token, cancellationToken);
                externalIdentity = command.Username;
                break;

            default:
                throw new AuthException(AuthErrorCatalog.IdentitySourceTypeMismatch);
        }

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

        var principal = await sender.Send(new BuildPrincipalQuery(user.Id, command.Scopes), cancellationToken);
        return new CredentialValidationResult.Success(principal);
    }
}
