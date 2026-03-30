using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IpAddress).IsRequired().HasMaxLength(45);
        builder.Property(x => x.UserAgent).IsRequired().HasMaxLength(500);
        builder.Property(x => x.AuthMethod).IsRequired().HasMaxLength(32);
        builder.Property(x => x.IsRevoked).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.RevokedReason).HasMaxLength(100);
        builder.Ignore(x => x.IsActive);
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_user_sessions_UserId");
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt })
            .HasFilter("\"IsRevoked\" = false")
            .HasDatabaseName("IX_user_sessions_UserId_Active");
        builder.HasIndex(x => x.CreatedAt)
            .IsDescending();
        builder.HasIndex(x => x.ApplicationId)
            .HasDatabaseName("IX_user_sessions_ApplicationId");
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Application)
            .WithMany()
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
