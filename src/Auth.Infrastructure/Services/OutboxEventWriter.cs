using System.Text.Json;
using Auth.Domain;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure;

public interface IOutboxEventWriter
{
    void AddRoleChanged(Guid roleId, string action, IReadOnlyCollection<Guid>? permissionIds = null);
    void AddPermissionChanged(Guid permissionId, string action);
}

public sealed class OutboxEventWriter(AuthDbContext dbContext, IOptions<IntegrationOptions> options) : IOutboxEventWriter
{
    private readonly string _roleChangedTopic = options.Value.Kafka.Topics.RoleChanged;
    private readonly string _permissionChangedTopic = options.Value.Kafka.Topics.PermissionChanged;

    public void AddRoleChanged(Guid roleId, string action, IReadOnlyCollection<Guid>? permissionIds = null)
    {
        var payload = JsonSerializer.Serialize(new
        {
            roleId,
            action,
            permissionIds,
            occurredAtUtc = DateTime.UtcNow
        });

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Topic = _roleChangedTopic,
            Key = roleId.ToString("D"),
            Payload = payload
        });
    }

    public void AddPermissionChanged(Guid permissionId, string action)
    {
        var payload = JsonSerializer.Serialize(new
        {
            permissionId,
            action,
            occurredAtUtc = DateTime.UtcNow
        });

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Topic = _permissionChangedTopic,
            Key = permissionId.ToString("D"),
            Payload = payload
        });
    }
}
