using Auth.Application;
using Auth.Application.NotificationTemplates.Queries.GetNotificationTemplateById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.NotificationTemplates.Queries.GetNotificationTemplateById;

internal sealed class GetNotificationTemplateByIdQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetNotificationTemplateByIdQuery, NotificationTemplateDto?>
{
    public async Task<NotificationTemplateDto?> Handle(GetNotificationTemplateByIdQuery query, CancellationToken cancellationToken) =>
        await dbContext.NotificationTemplates.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new NotificationTemplateDto(
                x.Id,
                x.Type.ToString(),
                x.Locale,
                x.Subject,
                x.Body))
            .FirstOrDefaultAsync(cancellationToken);
}
