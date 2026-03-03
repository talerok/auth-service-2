using System.Security.Claims;
using System.Text.Json;
using Auth.Application.Workspaces.Queries.BuildWorkspaceMasks;
using Auth.Domain;
using MediatR;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Infrastructure.Oidc;

internal static class OidcPrincipalFactory
{
    private const string OidcServerScheme = "OpenIddict.Server.AspNetCore";

    public static async Task<ClaimsPrincipal> CreateUserPrincipalAsync(
        User user, IEnumerable<string> scopes, ISender sender, CancellationToken cancellationToken)
    {
        var scopeList = scopes.ToList();

        var identity = new ClaimsIdentity(OidcServerScheme, Claims.Name, Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Name, user.FullName);
        identity.SetClaim(Claims.PreferredUsername, user.Username);

        if (scopeList.Contains(Scopes.Email))
            identity.SetClaim(Claims.Email, user.Email);

        if (scopeList.Contains(Scopes.Phone) && !string.IsNullOrWhiteSpace(user.Phone))
            identity.SetClaim(Claims.PhoneNumber, user.Phone);

        if (scopeList.Contains("ws"))
        {
            var masks = await sender.Send(new BuildWorkspaceMasksQuery(user.Id), cancellationToken);
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
            Claims.Email => [Destinations.IdentityToken],
            Claims.PhoneNumber => [Destinations.IdentityToken],
            "ws" => [Destinations.AccessToken],
            _ => [Destinations.AccessToken]
        });

        return principal;
    }
}
