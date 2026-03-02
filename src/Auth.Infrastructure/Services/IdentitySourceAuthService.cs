using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure;

internal sealed class IdentitySourceAuthService(
    AuthDbContext dbContext,
    IAuthService authService,
    IOidcGrantService oidcGrantService,
    IOidcTokenValidator tokenValidator) : IIdentitySourceAuthService
{
    public async Task<PasswordGrantResult> AuthenticateAsync(
        string identitySourceName, string token, IReadOnlyCollection<string> scopes, CancellationToken cancellationToken)
    {
        var source = await dbContext.IdentitySources
            .Include(x => x.OidcConfig)
            .FirstOrDefaultAsync(x => x.Name == identitySourceName, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        if (!source.IsEnabled)
            throw new AuthException(AuthErrorCatalog.IdentitySourceDisabled);

        if (source.Type != IdentitySourceType.Oidc || source.OidcConfig is null)
            throw new AuthException(AuthErrorCatalog.IdentitySourceTypeMismatch);

        var sub = await tokenValidator.ValidateAndGetSubjectAsync(
            source.OidcConfig.Authority, source.OidcConfig.ClientId, token, cancellationToken);

        var link = await dbContext.IdentitySourceLinks
            .FirstOrDefaultAsync(x => x.IdentitySourceId == source.Id && x.ExternalIdentity == sub, cancellationToken)
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
