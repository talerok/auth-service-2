using System.Security.Claims;
using Auth.Application;
using Auth.Application.Oidc.Commands.HandleClientCredentialsGrant;
using Auth.Application.Workspaces.Queries.BuildServiceAccountWorkspaceMasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Infrastructure.Oidc.Commands.HandleClientCredentialsGrant;

internal sealed class HandleClientCredentialsGrantCommandHandler(
    ISender sender,
    AuthDbContext dbContext) : IRequestHandler<HandleClientCredentialsGrantCommand, ClaimsPrincipal>
{
    public async Task<ClaimsPrincipal> Handle(HandleClientCredentialsGrantCommand command, CancellationToken cancellationToken)
    {
        var serviceAccount = await dbContext.ServiceAccounts
            .FirstOrDefaultAsync(x => x.ClientId == command.ClientId, cancellationToken);

        if (serviceAccount is null)
            throw new AuthException(AuthErrorCatalog.ApplicationNotFound);

        if (!serviceAccount.IsActive)
            throw new AuthException(AuthErrorCatalog.ApplicationInactive);

        var scopeList = command.Scopes.ToList();
        var identity = new ClaimsIdentity(OidcPrincipalFactory.OidcServerScheme, Claims.Name, Claims.Role);

        identity.SetClaim(Claims.Subject, serviceAccount.Id.ToString());
        identity.SetClaim(Claims.Name, serviceAccount.Name);
        identity.SetClaim(Claims.PreferredUsername, serviceAccount.ClientId);

        var accessibleMasks = await OidcPrincipalFactory.ResolveWorkspaceMasksAsync(
            OidcConstants.ExtractWorkspaceCodes(scopeList),
            saId => sender.Send(new BuildServiceAccountWorkspaceMasksQuery(saId), cancellationToken),
            serviceAccount.Id);
        OidcPrincipalFactory.ApplyWorkspaceClaims(identity, accessibleMasks);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopeList);
        OidcPrincipalFactory.ApplyWorkspaceAudiences(principal, accessibleMasks);

        principal.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Name => [Destinations.IdentityToken],
            Claims.PreferredUsername => [Destinations.IdentityToken],
            "ws" => [Destinations.AccessToken],
            _ => [Destinations.AccessToken]
        });

        return principal;
    }
}
