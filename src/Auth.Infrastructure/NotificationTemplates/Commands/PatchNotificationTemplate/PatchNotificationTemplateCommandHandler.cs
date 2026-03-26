using Auth.Application;
using Auth.Application.NotificationTemplates.Commands.PatchNotificationTemplate;
using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.NotificationTemplates.Commands.PatchNotificationTemplate;

internal sealed class PatchNotificationTemplateCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IAuditContext auditContext) : IRequestHandler<PatchNotificationTemplateCommand, NotificationTemplateDto?>
{
    public async Task<NotificationTemplateDto?> Handle(PatchNotificationTemplateCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.NotificationTemplates
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (entity is null)
            return null;

        if (command.Type is not null)
            entity.Type = Enum.Parse<NotificationTemplateType>(command.Type, true);
        if (command.Locale is not null)
            entity.Locale = command.Locale;
        if (command.Subject is not null)
            entity.Subject = command.Subject;
        if (command.Body is not null)
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
