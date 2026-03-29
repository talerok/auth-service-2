using System.Security.Cryptography;
using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.ServiceAccounts.Commands.CreateServiceAccount;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OidcPermissions = OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace Auth.Infrastructure.ServiceAccounts.Commands.CreateServiceAccount;

internal sealed class CreateServiceAccountCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IOpenIddictApplicationManager appManager,
    IAuditContext auditContext) : IRequestHandler<CreateServiceAccountCommand, CreateServiceAccountResponse>
{
    public async Task<CreateServiceAccountResponse> Handle(CreateServiceAccountCommand command, CancellationToken cancellationToken)
    {
        var clientId = $"sa-{Guid.NewGuid():N}";
        var clientSecret = GenerateSecret();

        var serviceAccount = new Domain.ServiceAccount
        {
            Id = command.EntityId,
            Name = command.Name,
            Description = command.Description,
            ClientId = clientId,
            IsActive = command.IsActive,
            AccessTokenLifetimeMinutes = command.AccessTokenLifetimeMinutes
        };
        if (command.Audiences is { Count: > 0 })
            serviceAccount.SetAudiences(command.Audiences.ToList());

        dbContext.ServiceAccounts.Add(serviceAccount);
        auditContext.Details = AuditDiff.CaptureState(dbContext.Entry(serviceAccount));
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.ServiceAccount, EntityId = serviceAccount.Id, Operation = IndexOperation.Index }, cancellationToken);
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
            OidcPermissions.Prefixes.Scope + "ws:*"
        });
        await appManager.CreateAsync(descriptor, cancellationToken);

        var dto = MapToDto(serviceAccount);
        return new CreateServiceAccountResponse(dto, clientSecret);
    }

    private static ServiceAccountDto MapToDto(Domain.ServiceAccount sa) =>
        new(sa.Id, sa.Name, sa.Description, sa.ClientId, sa.IsActive, sa.Audiences, sa.AccessTokenLifetimeMinutes);

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
