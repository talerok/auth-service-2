using Auth.Application;
using Auth.Application.NotificationTemplates.Commands.UpdateNotificationTemplate;
using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.NotificationTemplates.Commands.UpdateNotificationTemplate;

internal sealed class UpdateNotificationTemplateCommandHandler(
    AuthDbContext dbContext,
    IAuditContext auditContext) : IRequestHandler<UpdateNotificationTemplateCommand, NotificationTemplateDto?>
{
    public async Task<NotificationTemplateDto?> Handle(UpdateNotificationTemplateCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TwoFactorChannel>(command.Channel, true, out var parsed))
            return null;

        var entity = await dbContext.NotificationTemplates
            .FirstOrDefaultAsync(x => x.Channel == parsed, cancellationToken);

        if (entity is null)
            return null;

        entity.Subject = command.Subject;
        entity.Body = command.Body;

        auditContext.EntityId = entity.Id;
        var changes = AuditDiff.CaptureChanges(dbContext.Entry(entity));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new NotificationTemplateDto(
            entity.Id,
            entity.Channel.ToString().ToLowerInvariant(),
            entity.Subject,
            entity.Body);
    }
}
