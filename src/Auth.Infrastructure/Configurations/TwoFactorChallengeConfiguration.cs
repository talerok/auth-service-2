using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class TwoFactorChallengeConfiguration : IEntityTypeConfiguration<TwoFactorChallenge>
{
    public void Configure(EntityTypeBuilder<TwoFactorChallenge> builder)
    {
        builder.ToTable("two_factor_challenges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Purpose).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Channel)
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => Enum.Parse<TwoFactorChannel>(value, true))
            .IsRequired()
            .HasMaxLength(16);
        builder.Property(x => x.OtpHash).IsRequired().HasMaxLength(200);
        builder.Property(x => x.OtpSalt).IsRequired().HasMaxLength(120);
        builder.Property(x => x.OtpEncrypted).IsRequired().HasMaxLength(512);
        builder.Property(x => x.DeliveryStatus).IsRequired().HasMaxLength(32);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.Purpose });
        builder.HasOne(x => x.User)
            .WithMany(x => x.TwoFactorChallenges)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
