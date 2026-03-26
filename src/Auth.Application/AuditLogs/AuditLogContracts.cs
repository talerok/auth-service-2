using System.Text.Json;
using Auth.Domain;

namespace Auth.Application;

public sealed record AuditLogDto(
    Guid Id,
    DateTime Timestamp,
    Guid? ActorId,
    string? ActorName,
    string ActorType,
    string EntityType,
    Guid EntityId,
    string Action,
    Dictionary<string, object?>? Details,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId)
{
    public static string CamelCase<TEnum>(TEnum value) where TEnum : struct, Enum =>
        JsonNamingPolicy.CamelCase.ConvertName(value.ToString());
}
