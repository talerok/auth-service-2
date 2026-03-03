using System.Security.Claims;
using System.Text.Json;
using Auth.Application;
using Auth.Application.Oidc.Commands.HandleClientCredentialsGrant;
using Auth.Application.Workspaces.Queries.BuildApiClientWorkspaceMasks;
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
        var apiClient = await dbContext.ApiClients
            .FirstOrDefaultAsync(x => x.ClientId == command.ClientId, cancellationToken);

        if (apiClient is null)
            throw new AuthException(AuthErrorCatalog.ApiClientNotFound);

        if (!apiClient.IsActive)
            throw new AuthException(AuthErrorCatalog.ApiClientInactive);

        var scopeList = command.Scopes.ToList();
        var identity = new ClaimsIdentity(OidcServerScheme, Claims.Name, Claims.Role);

        identity.SetClaim(Claims.Subject, apiClient.Id.ToString());
        identity.SetClaim(Claims.Name, apiClient.Name);
        identity.SetClaim(Claims.PreferredUsername, apiClient.ClientId);

        if (scopeList.Contains("ws"))
        {
            var masks = await sender.Send(new BuildApiClientWorkspaceMasksQuery(apiClient.Id), cancellationToken);
            var wsPayload = masks.ToDictionary(x => x.Key, x => Convert.ToBase64String(x.Value));
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
