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
        var clientSecret = GenerateSecret();

        var apiClient = new ApiClient
        {
            Name = command.Name,
            Description = command.Description,
            ClientId = clientId,
            IsActive = command.IsActive
        };

        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync(cancellationToken);

        await appManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = command.Name,
            ClientType = ClientTypes.Confidential,
            Permissions =
            {
                OidcPermissions.Endpoints.Token,
                OidcPermissions.GrantTypes.ClientCredentials,
                OidcPermissions.Scopes.Email,
                OidcPermissions.Scopes.Profile,
                OidcPermissions.Prefixes.Scope + "ws"
            }
        }, cancellationToken);

        var dto = new ApiClientDto(apiClient.Id, apiClient.Name, apiClient.Description, apiClient.ClientId, apiClient.IsActive);
        await searchIndexService.IndexApiClientAsync(dto, cancellationToken);
        return new CreateApiClientResponse(dto, clientSecret);
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
