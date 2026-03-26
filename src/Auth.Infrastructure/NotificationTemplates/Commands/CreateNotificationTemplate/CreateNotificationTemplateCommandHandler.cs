using Auth.Application;
using Auth.Application.NotificationTemplates.Commands.CreateNotificationTemplate;
using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using MediatR;

namespace Auth.Infrastructure.NotificationTemplates.Commands.CreateNotificationTemplate;

internal sealed class CreateNotificationTemplateCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IAuditContext auditContext) : IRequestHandler<CreateNotificationTemplateCommand, NotificationTemplateDto>
{
    public async Task<NotificationTemplateDto> Handle(CreateNotificationTemplateCommand command, CancellationToken cancellationToken)
    {
        var type = Enum.Parse<NotificationTemplateType>(command.Type, true);

        var entity = new NotificationTemplate
        {
            Id = command.EntityId,
            Type = type,
            Locale = command.Locale,
            Subject = command.Subject,
            Body = command.Body
        };

        dbContext.NotificationTemplates.Add(entity);
        auditContext.Details = AuditDiff.CaptureState(dbContext.Entry(entity));
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new NotificationTemplateDto(entity.Id, entity.Type.ToString(), entity.Locale, entity.Subject, entity.Body);
        await searchIndexService.IndexNotificationTemplateAsync(dto, cancellationToken);
        return dto;
    }
}
