using Auth.Application;
using Auth.Application.ApiClients.Commands.PatchApiClient;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OidcPermissions = OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace Auth.Infrastructure.ApiClients.Commands.PatchApiClient;

internal sealed class PatchApiClientCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictApplicationManager appManager) : IRequestHandler<PatchApiClientCommand, ApiClientDto?>
{
    public async Task<ApiClientDto?> Handle(PatchApiClientCommand command, CancellationToken cancellationToken)
    {
        var apiClient = await dbContext.ApiClients.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (apiClient is null)
            return null;

        if (command.Name is not null)
            apiClient.Name = command.Name;

        if (command.Description is not null)
            apiClient.Description = command.Description;

        if (command.IsActive.HasValue)
            apiClient.IsActive = command.IsActive.Value;

        if (command.Type.HasValue)
            apiClient.Type = command.Type.Value;

        if (command.IsConfidential.HasValue)
            apiClient.IsConfidential = command.IsConfidential.Value;

        if (command.LogoUrl is not null)
            apiClient.LogoUrl = command.LogoUrl;

        if (command.HomepageUrl is not null)
            apiClient.HomepageUrl = command.HomepageUrl;

        if (command.RedirectUris is not null)
            apiClient.RedirectUris = command.RedirectUris;

        if (command.PostLogoutRedirectUris is not null)
            apiClient.PostLogoutRedirectUris = command.PostLogoutRedirectUris;

        apiClient.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var needsOidcSync = command.Name is not null
                            || command.Type.HasValue
                            || command.IsConfidential.HasValue
                            || command.RedirectUris is not null
                            || command.PostLogoutRedirectUris is not null
                            || command.ConsentType is not null;

        if (needsOidcSync)
            await SyncOpenIddictApplication(apiClient, command, cancellationToken);

        var dto = MapToDto(apiClient);
        await searchIndexService.IndexApiClientAsync(dto, cancellationToken);
        return dto;
    }

    private async Task SyncOpenIddictApplication(
        ApiClient apiClient, PatchApiClientCommand command, CancellationToken cancellationToken)
    {
        var oidcApp = await appManager.FindByClientIdAsync(apiClient.ClientId, cancellationToken);
        if (oidcApp is null)
            return;

        var descriptor = new OpenIddictApplicationDescriptor();
        await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);

        if (command.Name is not null)
            descriptor.DisplayName = apiClient.Name;

        if (command.IsConfidential.HasValue)
            descriptor.ClientType = apiClient.IsConfidential ? ClientTypes.Confidential : ClientTypes.Public;

        // If type changed, rebuild permissions entirely
        if (command.Type.HasValue)
        {
            descriptor.Permissions.Clear();
            if (apiClient.Type == ApiClientType.OAuthApplication)
            {
                descriptor.Permissions.UnionWith(new[]
                {
                    OidcPermissions.Endpoints.Authorization,
                    OidcPermissions.Endpoints.Token,
                    OidcPermissions.Endpoints.EndSession,
                    OidcPermissions.Endpoints.Revocation,
                    OidcPermissions.GrantTypes.AuthorizationCode,
                    OidcPermissions.GrantTypes.RefreshToken,
                    OidcPermissions.ResponseTypes.Code,
                    OidcPermissions.Scopes.Email,
                    OidcPermissions.Scopes.Profile,
                    OidcPermissions.Prefixes.Scope + "ws"
                });
                descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
            }
            else
            {
                descriptor.Permissions.UnionWith(new[]
                {
                    OidcPermissions.Endpoints.Token,
                    OidcPermissions.GrantTypes.ClientCredentials,
                    OidcPermissions.Scopes.Email,
                    OidcPermissions.Scopes.Profile,
                    OidcPermissions.Prefixes.Scope + "ws"
                });
                descriptor.RedirectUris.Clear();
                descriptor.PostLogoutRedirectUris.Clear();
            }
        }

        if (command.RedirectUris is not null)
        {
            descriptor.RedirectUris.Clear();
            foreach (var uri in apiClient.RedirectUris)
                descriptor.RedirectUris.Add(new Uri(uri));
        }

        if (command.PostLogoutRedirectUris is not null)
        {
            descriptor.PostLogoutRedirectUris.Clear();
            foreach (var uri in apiClient.PostLogoutRedirectUris)
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
        }

        if (command.ConsentType is not null)
        {
            descriptor.ConsentType = command.ConsentType switch
            {
                "implicit" => ConsentTypes.Implicit,
                _ => ConsentTypes.Explicit
            };
        }

        await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
    }

    private static ApiClientDto MapToDto(ApiClient c) =>
        new(c.Id, c.Name, c.Description, c.ClientId, c.IsActive,
            c.Type, c.IsConfidential, c.LogoUrl, c.HomepageUrl,
            c.RedirectUris, c.PostLogoutRedirectUris);
}
