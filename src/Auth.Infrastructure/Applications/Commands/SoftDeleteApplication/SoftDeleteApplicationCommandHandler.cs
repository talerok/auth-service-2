using Auth.Application;
using Auth.Application.Applications.Commands.SoftDeleteApplication;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.Applications.Commands.SoftDeleteApplication;

internal sealed class SoftDeleteApplicationCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IOpenIddictApplicationManager appManager,
    IAuditContext auditContext) : IRequestHandler<SoftDeleteApplicationCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteApplicationCommand command, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (application is null)
            return false;

        auditContext.Details = new Dictionary<string, object?> { ["name"] = application.Name, ["clientId"] = application.ClientId };
        application.SoftDelete();
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Application, EntityId = application.Id, Operation = IndexOperation.Delete }, cancellationToken);
        await eventBus.PublishAsync(new CorsOriginsChangedEvent(), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var oidcApp = await appManager.FindByClientIdAsync(application.ClientId, cancellationToken);
        if (oidcApp is not null)
            await appManager.DeleteAsync(oidcApp, cancellationToken);

        return true;
    }
}
