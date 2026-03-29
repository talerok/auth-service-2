using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.ServiceAccounts.Commands.PatchServiceAccount;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.ServiceAccounts.Commands.PatchServiceAccount;

internal sealed class PatchServiceAccountCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IOpenIddictApplicationManager appManager,
    IAuditContext auditContext) : IRequestHandler<PatchServiceAccountCommand, ServiceAccountDto?>
{
    public async Task<ServiceAccountDto?> Handle(PatchServiceAccountCommand command, CancellationToken cancellationToken)
    {
        var serviceAccount = await dbContext.ServiceAccounts.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (serviceAccount is null)
            return null;

        if (command.Name.HasValue)
            serviceAccount.Name = command.Name.Value!;

        if (command.Description.HasValue)
            serviceAccount.Description = command.Description.Value!;

        if (command.IsActive.HasValue)
            serviceAccount.IsActive = command.IsActive.Value;

        if (command.Audiences.HasValue)
            serviceAccount.SetAudiences(command.Audiences.Value!.ToList());

        if (command.AccessTokenLifetimeMinutes.HasValue)
            serviceAccount.AccessTokenLifetimeMinutes = command.AccessTokenLifetimeMinutes.Value;

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(serviceAccount));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.ServiceAccount, EntityId = serviceAccount.Id, Operation = IndexOperation.Index }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (command.Name.HasValue)
        {
            var oidcApp = await appManager.FindByClientIdAsync(serviceAccount.ClientId, cancellationToken);
            if (oidcApp is not null)
            {
                var descriptor = new OpenIddictApplicationDescriptor();
                await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);
                descriptor.DisplayName = serviceAccount.Name;
                await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
            }
        }

        var dto = MapToDto(serviceAccount);
        return dto;
    }

    private static ServiceAccountDto MapToDto(Domain.ServiceAccount sa) =>
        new(sa.Id, sa.Name, sa.Description, sa.ClientId, sa.IsActive, sa.Audiences, sa.AccessTokenLifetimeMinutes);
}
