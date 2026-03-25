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
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object?>>(v, JsonOptions));

        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.CorrelationId).HasMaxLength(64);

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.ActorId);
        builder.HasIndex(x => x.Timestamp).IsDescending();
    }
}
