namespace Auth.Application;

public interface INotificationTemplateService
{
    Task<IReadOnlyCollection<NotificationTemplateDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<NotificationTemplateDto?> GetByChannelAsync(string channel, CancellationToken cancellationToken);
    Task<NotificationTemplateDto?> UpdateByChannelAsync(string channel, UpdateNotificationTemplateRequest request, CancellationToken cancellationToken);
}
