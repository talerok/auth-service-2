using Auth.Application;
using Auth.Application.NotificationTemplates.Queries.GetNotificationTemplateByChannel;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.NotificationTemplates.Queries.GetNotificationTemplateByChannel;

internal sealed class GetNotificationTemplateByChannelQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetNotificationTemplateByChannelQuery, NotificationTemplateDto?>
{
    public async Task<NotificationTemplateDto?> Handle(GetNotificationTemplateByChannelQuery query, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TwoFactorChannel>(query.Channel, true, out var parsed))
            return null;

        return await dbContext.NotificationTemplates.AsNoTracking()
            .Where(x => x.Channel == parsed)
            .Select(x => new NotificationTemplateDto(
                x.Id,
                x.Channel.ToString().ToLowerInvariant(),
                x.Subject,
                x.Body))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
