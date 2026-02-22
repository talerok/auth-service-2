using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class PasswordChangeChallengeConfiguration : IEntityTypeConfiguration<PasswordChangeChallenge>
{
    public void Configure(EntityTypeBuilder<PasswordChangeChallenge> builder)
    {
        builder.ToTable("password_change_challenges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.IsUsed).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UsedAt);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.UserId);
    }
}
