using System.Security.Claims;
using System.Text.Json;
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
    private const string OidcServerScheme = "OpenIddict.Server.AspNetCore";

    public async Task<ClaimsPrincipal> Handle(HandleClientCredentialsGrantCommand command, CancellationToken cancellationToken)
    {
        var serviceAccount = await dbContext.ServiceAccounts
            .FirstOrDefaultAsync(x => x.ClientId == command.ClientId, cancellationToken);

        if (serviceAccount is null)
            throw new AuthException(AuthErrorCatalog.ApplicationNotFound);

        if (!serviceAccount.IsActive)
            throw new AuthException(AuthErrorCatalog.ApplicationInactive);

        var scopeList = command.Scopes.ToList();
        var identity = new ClaimsIdentity(OidcServerScheme, Claims.Name, Claims.Role);

        identity.SetClaim(Claims.Subject, serviceAccount.Id.ToString());
        identity.SetClaim(Claims.Name, serviceAccount.Name);
        identity.SetClaim(Claims.PreferredUsername, serviceAccount.ClientId);

        if (scopeList.Contains("ws"))
        {
            var masks = await sender.Send(new BuildServiceAccountWorkspaceMasksQuery(serviceAccount.Id), cancellationToken);
            var wsPayload = masks.ToDictionary(
                ws => ws.Key,
                ws => ws.Value.ToDictionary(d => d.Key, d => Convert.ToBase64String(d.Value)));
            identity.AddClaim(new Claim("ws", JsonSerializer.Serialize(wsPayload)));
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopeList);

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
