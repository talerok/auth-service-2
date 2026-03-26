using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => Enum.Parse<NotificationTemplateType>(value, true))
            .HasMaxLength(32);
        builder.Property(x => x.Locale).IsRequired().HasMaxLength(16).HasDefaultValue("en-US");
        builder.Property(x => x.Subject).HasMaxLength(500);
        builder.Property(x => x.Body).HasColumnType("text");
        builder.HasQueryFilter(x => x.DeletedAt == null);
        builder.HasIndex(x => new { x.Type, x.Locale }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
    }
}
