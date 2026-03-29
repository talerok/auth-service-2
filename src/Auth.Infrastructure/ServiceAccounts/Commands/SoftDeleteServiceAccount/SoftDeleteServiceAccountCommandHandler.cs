using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.ServiceAccounts.Commands.SoftDeleteServiceAccount;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.ServiceAccounts.Commands.SoftDeleteServiceAccount;

internal sealed class SoftDeleteServiceAccountCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IOpenIddictApplicationManager appManager,
    IAuditContext auditContext) : IRequestHandler<SoftDeleteServiceAccountCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteServiceAccountCommand command, CancellationToken cancellationToken)
    {
        var serviceAccount = await dbContext.ServiceAccounts.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (serviceAccount is null)
            return false;

        auditContext.Details = new Dictionary<string, object?> { ["name"] = serviceAccount.Name, ["clientId"] = serviceAccount.ClientId };
        serviceAccount.SoftDelete();
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.ServiceAccount, EntityId = serviceAccount.Id, Operation = IndexOperation.Delete }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var oidcApp = await appManager.FindByClientIdAsync(serviceAccount.ClientId, cancellationToken);
        if (oidcApp is not null)
            await appManager.DeleteAsync(oidcApp, cancellationToken);

        return true;
    }
}
