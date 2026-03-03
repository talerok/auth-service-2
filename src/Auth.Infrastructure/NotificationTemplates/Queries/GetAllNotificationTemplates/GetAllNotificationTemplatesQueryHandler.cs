using Auth.Application;
using Auth.Application.NotificationTemplates.Queries.GetAllNotificationTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.NotificationTemplates.Queries.GetAllNotificationTemplates;

internal sealed class GetAllNotificationTemplatesQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetAllNotificationTemplatesQuery, IReadOnlyCollection<NotificationTemplateDto>>
{
    public async Task<IReadOnlyCollection<NotificationTemplateDto>> Handle(GetAllNotificationTemplatesQuery query, CancellationToken cancellationToken) =>
        await dbContext.NotificationTemplates.AsNoTracking()
            .Select(x => new NotificationTemplateDto(
                x.Id,
                x.Channel.ToString().ToLowerInvariant(),
                x.Subject,
                x.Body))
            .ToListAsync(cancellationToken);
}
