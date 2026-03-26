using Auth.Application;
using Auth.Application.NotificationTemplates.Commands.UpdateNotificationTemplate;
using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.NotificationTemplates.Commands.UpdateNotificationTemplate;

internal sealed class UpdateNotificationTemplateCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IAuditContext auditContext) : IRequestHandler<UpdateNotificationTemplateCommand, NotificationTemplateDto?>
{
    public async Task<NotificationTemplateDto?> Handle(UpdateNotificationTemplateCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.NotificationTemplates
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (entity is null)
            return null;

        var type = Enum.Parse<NotificationTemplateType>(command.Type, true);

        entity.Type = type;
        entity.Locale = command.Locale;
        entity.Subject = command.Subject;
        entity.Body = command.Body;

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(entity));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new NotificationTemplateDto(entity.Id, entity.Type.ToString(), entity.Locale, entity.Subject, entity.Body);
        await searchIndexService.IndexNotificationTemplateAsync(dto, cancellationToken);
        return dto;
    }
}
