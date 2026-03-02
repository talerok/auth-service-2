using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure;

internal sealed class IdentitySourceAuthService(
    AuthDbContext dbContext,
    IAuthService authService,
    IOidcGrantService oidcGrantService,
    IOidcTokenValidator tokenValidator,
    ILdapAuthenticator ldapAuthenticator) : IIdentitySourceAuthService
{
    public async Task<PasswordGrantResult> AuthenticateAsync(
        string identitySourceName, string? username, string token, IReadOnlyCollection<string> scopes, CancellationToken cancellationToken)
    {
        var source = await dbContext.IdentitySources
            .Include(x => x.OidcConfig)
            .Include(x => x.LdapConfig)
            .FirstOrDefaultAsync(x => x.Name == identitySourceName, cancellationToken)
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
                    source.OidcConfig.Authority, source.OidcConfig.ClientId, token, cancellationToken);
                break;

            case IdentitySourceType.Ldap:
                if (source.LdapConfig is null)
                    throw new AuthException(AuthErrorCatalog.IdentitySourceTypeMismatch);

                if (string.IsNullOrWhiteSpace(username))
                    throw new AuthException(AuthErrorCatalog.IdentitySourceUsernameRequired);

                await ldapAuthenticator.AuthenticateAsync(source.LdapConfig, username, token, cancellationToken);
                externalIdentity = username;
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

        if (user.MustChangePassword)
        {
            var challenge = await authService.CreatePasswordChangeChallengeAsync(user.Id, cancellationToken);
            return new PasswordGrantResult.PasswordChangeRequired(challenge.Id);
        }

        if (user.TwoFactorEnabled)
        {
            var mfaChallenge = await authService.CreateLoginChallengeAsync(
                user.Id, user.TwoFactorChannel!.Value, cancellationToken);
            return new PasswordGrantResult.MfaRequired(mfaChallenge.Id, mfaChallenge.Channel);
        }

        var principal = await oidcGrantService.BuildPrincipalAsync(user.Id, scopes, cancellationToken);
        return new PasswordGrantResult.Success(principal);
    }
}
