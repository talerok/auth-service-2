using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.ServiceAccounts.Commands.UpdateServiceAccount;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Infrastructure.ServiceAccounts.Commands.UpdateServiceAccount;

internal sealed class UpdateServiceAccountCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IOpenIddictApplicationManager appManager,
    IAuditContext auditContext) : IRequestHandler<UpdateServiceAccountCommand, ServiceAccountDto?>
{
    public async Task<ServiceAccountDto?> Handle(UpdateServiceAccountCommand command, CancellationToken cancellationToken)
    {
        var serviceAccount = await dbContext.ServiceAccounts.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (serviceAccount is null)
            return null;

        serviceAccount.Name = command.Name;
        serviceAccount.Description = command.Description;
        serviceAccount.IsActive = command.IsActive;
        serviceAccount.AccessTokenLifetimeMinutes = command.AccessTokenLifetimeMinutes;
        if (command.Audiences is not null)
            serviceAccount.SetAudiences(command.Audiences.ToList());

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(serviceAccount));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.ServiceAccount, EntityId = serviceAccount.Id, Operation = IndexOperation.Index }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var oidcApp = await appManager.FindByClientIdAsync(serviceAccount.ClientId, cancellationToken);
        if (oidcApp is not null)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);
            descriptor.DisplayName = command.Name;
            await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
        }

        var dto = MapToDto(serviceAccount);
        return dto;
    }

    private static ServiceAccountDto MapToDto(Domain.ServiceAccount sa) =>
        new(sa.Id, sa.Name, sa.Description, sa.ClientId, sa.IsActive, sa.Audiences, sa.AccessTokenLifetimeMinutes);
}
