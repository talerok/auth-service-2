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
        builder.Property(x => x.Channel)
            .IsRequired()
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => Enum.Parse<TwoFactorChannel>(value, true))
            .HasMaxLength(16);
        builder.Property(x => x.Subject).HasMaxLength(500);
        builder.Property(x => x.HtmlBody).HasColumnType("text");
        builder.Property(x => x.TextBody).HasMaxLength(2000);
        builder.HasQueryFilter(x => x.DeletedAt == null);
        builder.HasIndex(x => x.Channel).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
    }
}
