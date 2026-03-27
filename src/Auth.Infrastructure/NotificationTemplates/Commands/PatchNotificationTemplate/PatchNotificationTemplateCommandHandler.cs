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

        if (command.Type.HasValue)
            entity.Type = Enum.Parse<NotificationTemplateType>(command.Type.Value!, true);
        if (command.Locale.HasValue)
            entity.Locale = command.Locale.Value!;
        if (command.Subject.HasValue)
            entity.Subject = command.Subject.Value!;
        if (command.Body.HasValue)
            entity.Body = command.Body.Value!;

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(entity));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new NotificationTemplateDto(entity.Id, entity.Type.ToString(), entity.Locale, entity.Subject, entity.Body);
        await searchIndexService.IndexNotificationTemplateAsync(dto, cancellationToken);
        return dto;
    }
}
