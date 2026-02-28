using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure;

public sealed class NotificationTemplateService(AuthDbContext dbContext) : INotificationTemplateService
{
    public async Task<IReadOnlyCollection<NotificationTemplateDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.NotificationTemplates.AsNoTracking()
            .Select(x => new NotificationTemplateDto(
                x.Id,
                x.Channel.ToString().ToLowerInvariant(),
                x.Subject,
                x.Body))
            .ToListAsync(cancellationToken);

    public async Task<NotificationTemplateDto?> GetByChannelAsync(string channel, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TwoFactorChannel>(channel, true, out var parsed))
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

    public async Task<NotificationTemplateDto?> UpdateByChannelAsync(
        string channel,
        UpdateNotificationTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TwoFactorChannel>(channel, true, out var parsed))
            return null;

        var entity = await dbContext.NotificationTemplates
            .FirstOrDefaultAsync(x => x.Channel == parsed, cancellationToken);

        if (entity is null)
            return null;

        entity.Subject = request.Subject;
        entity.Body = request.Body;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new NotificationTemplateDto(
            entity.Id,
            entity.Channel.ToString().ToLowerInvariant(),
            entity.Subject,
            entity.Body);
    }
}
