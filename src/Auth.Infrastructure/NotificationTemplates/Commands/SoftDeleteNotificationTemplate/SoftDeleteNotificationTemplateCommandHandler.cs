using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.NotificationTemplates.Commands.SoftDeleteNotificationTemplate;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.NotificationTemplates.Commands.SoftDeleteNotificationTemplate;

internal sealed class SoftDeleteNotificationTemplateCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<SoftDeleteNotificationTemplateCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteNotificationTemplateCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.NotificationTemplates
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (entity is null)
            return false;

        auditContext.Details = AuditDiff.CaptureState(dbContext.Entry(entity));
        entity.SoftDelete();
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.NotificationTemplate, EntityId = entity.Id, Operation = IndexOperation.Delete }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
