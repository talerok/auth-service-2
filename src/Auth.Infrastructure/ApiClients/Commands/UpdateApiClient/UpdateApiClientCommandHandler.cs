using Auth.Application;
using Auth.Application.ApiClients.Commands.UpdateApiClient;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OidcPermissions = OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace Auth.Infrastructure.ApiClients.Commands.UpdateApiClient;

internal sealed class UpdateApiClientCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictApplicationManager appManager) : IRequestHandler<UpdateApiClientCommand, ApiClientDto?>
{
    public async Task<ApiClientDto?> Handle(UpdateApiClientCommand command, CancellationToken cancellationToken)
    {
        var apiClient = await dbContext.ApiClients.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (apiClient is null)
            return null;

        apiClient.Name = command.Name;
        apiClient.Description = command.Description;
        apiClient.IsActive = command.IsActive;
        apiClient.Type = command.Type;
        apiClient.IsConfidential = command.IsConfidential;
        apiClient.LogoUrl = command.LogoUrl;
        apiClient.HomepageUrl = command.HomepageUrl;
        apiClient.RedirectUris = command.RedirectUris;
        apiClient.PostLogoutRedirectUris = command.PostLogoutRedirectUris;
        apiClient.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await SyncOpenIddictApplication(apiClient, command, cancellationToken);

        var dto = MapToDto(apiClient);
        await searchIndexService.IndexApiClientAsync(dto, cancellationToken);
        return dto;
    }

    private async Task SyncOpenIddictApplication(
        ApiClient apiClient, UpdateApiClientCommand command, CancellationToken cancellationToken)
    {
        var oidcApp = await appManager.FindByClientIdAsync(apiClient.ClientId, cancellationToken);
        if (oidcApp is null)
            return;

        var descriptor = new OpenIddictApplicationDescriptor();
        await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);

        descriptor.DisplayName = command.Name;
        descriptor.ClientType = command.IsConfidential ? ClientTypes.Confidential : ClientTypes.Public;

        // Rebuild permissions based on type
        descriptor.Permissions.Clear();
        if (command.Type == ApiClientType.OAuthApplication)
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

            descriptor.RedirectUris.Clear();
            foreach (var uri in command.RedirectUris)
                descriptor.RedirectUris.Add(new Uri(uri));

            descriptor.PostLogoutRedirectUris.Clear();
            foreach (var uri in command.PostLogoutRedirectUris)
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri));

            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
            descriptor.ConsentType = command.ConsentType switch
            {
                "implicit" => ConsentTypes.Implicit,
                _ => ConsentTypes.Explicit
            };
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
            descriptor.ConsentType = ConsentTypes.Implicit;
        }

        await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
    }

    private static ApiClientDto MapToDto(ApiClient c) =>
        new(c.Id, c.Name, c.Description, c.ClientId, c.IsActive,
            c.Type, c.IsConfidential, c.LogoUrl, c.HomepageUrl,
            c.RedirectUris, c.PostLogoutRedirectUris);
}
