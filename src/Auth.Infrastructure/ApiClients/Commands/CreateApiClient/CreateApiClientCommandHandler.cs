using System.Security.Cryptography;
using Auth.Application;
using Auth.Application.ApiClients.Commands.CreateApiClient;
using Auth.Domain;
using MediatR;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OidcPermissions = OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace Auth.Infrastructure.ApiClients.Commands.CreateApiClient;

internal sealed class CreateApiClientCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictApplicationManager appManager) : IRequestHandler<CreateApiClientCommand, CreateApiClientResponse>
{
    public async Task<CreateApiClientResponse> Handle(CreateApiClientCommand command, CancellationToken cancellationToken)
    {
        var clientId = $"ac-{Guid.NewGuid():N}";
        var isConfidential = command.IsConfidential;
        var clientSecret = isConfidential ? GenerateSecret() : null;

        var apiClient = new ApiClient
        {
            Name = command.Name,
            Description = command.Description,
            ClientId = clientId,
            IsActive = command.IsActive,
            Type = command.Type,
            IsConfidential = isConfidential,
            LogoUrl = command.LogoUrl,
            HomepageUrl = command.HomepageUrl,
            RedirectUris = command.RedirectUris ?? [],
            PostLogoutRedirectUris = command.PostLogoutRedirectUris ?? []
        };

        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync(cancellationToken);

        var descriptor = BuildDescriptor(command, clientId, clientSecret);
        await appManager.CreateAsync(descriptor, cancellationToken);

        var dto = MapToDto(apiClient);
        await searchIndexService.IndexApiClientAsync(dto, cancellationToken);
        return new CreateApiClientResponse(dto, clientSecret);
    }

    private static OpenIddictApplicationDescriptor BuildDescriptor(
        CreateApiClientCommand command, string clientId, string? clientSecret)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = command.Name,
            ClientType = command.IsConfidential ? ClientTypes.Confidential : ClientTypes.Public
        };

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

            foreach (var uri in command.RedirectUris ?? [])
                descriptor.RedirectUris.Add(new Uri(uri));

            foreach (var uri in command.PostLogoutRedirectUris ?? [])
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
        }

        return descriptor;
    }

    private static ApiClientDto MapToDto(ApiClient c) =>
        new(c.Id, c.Name, c.Description, c.ClientId, c.IsActive,
            c.Type, c.IsConfidential, c.LogoUrl, c.HomepageUrl,
            c.RedirectUris, c.PostLogoutRedirectUris);

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
