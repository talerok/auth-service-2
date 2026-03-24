using System.Security.Cryptography;
using Auth.Application;
using Auth.Application.ServiceAccounts.Commands.CreateServiceAccount;
using MediatR;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OidcPermissions = OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace Auth.Infrastructure.ServiceAccounts.Commands.CreateServiceAccount;

internal sealed class CreateServiceAccountCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictApplicationManager appManager) : IRequestHandler<CreateServiceAccountCommand, CreateServiceAccountResponse>
{
    public async Task<CreateServiceAccountResponse> Handle(CreateServiceAccountCommand command, CancellationToken cancellationToken)
    {
        var clientId = $"sa-{Guid.NewGuid():N}";
        var clientSecret = GenerateSecret();

        var serviceAccount = new Domain.ServiceAccount
        {
            Name = command.Name,
            Description = command.Description,
            ClientId = clientId,
            IsActive = command.IsActive
        };

        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync(cancellationToken);

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = command.Name,
            ClientType = ClientTypes.Confidential
        };
        descriptor.Permissions.UnionWith(new[]
        {
            OidcPermissions.Endpoints.Token,
            OidcPermissions.GrantTypes.ClientCredentials,
            OidcPermissions.Scopes.Email,
            OidcPermissions.Scopes.Profile
        });
        await appManager.CreateAsync(descriptor, cancellationToken);

        var dto = MapToDto(serviceAccount);
        await searchIndexService.IndexServiceAccountAsync(dto, cancellationToken);
        return new CreateServiceAccountResponse(dto, clientSecret);
    }

    private static ServiceAccountDto MapToDto(Domain.ServiceAccount sa) =>
        new(sa.Id, sa.Name, sa.Description, sa.ClientId, sa.IsActive);

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
