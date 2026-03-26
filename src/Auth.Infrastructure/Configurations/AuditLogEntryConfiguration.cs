using System.Text.Json;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log_entries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Timestamp).IsRequired();

        builder.Property(x => x.ActorName).HasMaxLength(200);

        builder.Property(x => x.ActorType)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => Enum.Parse<AuditActorType>(value, true));

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(100)
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => Enum.Parse<AuditEntityType>(value, true));

        builder.Property(x => x.EntityId).IsRequired();

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(100)
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => Enum.Parse<AuditAction>(value, true));

        builder.Property(x => x.Details)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                v => v == null ? null : DeserializeDetails(v));

        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.CorrelationId).HasMaxLength(64);

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.ActorId);
        builder.HasIndex(x => x.Timestamp).IsDescending();
    }

    private static Dictionary<string, object?>? DeserializeDetails(string? json)
    {
        if (json is null) return null;
        var raw = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions);
        if (raw is null) return null;

        var result = new Dictionary<string, object?>(raw.Count);
        foreach (var (key, value) in raw)
        {
            result[key] = value is JsonElement el ? el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Number => el.TryGetInt64(out var l) ? (object)l : el.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            } : value;
        }
        return result;
    }
}
