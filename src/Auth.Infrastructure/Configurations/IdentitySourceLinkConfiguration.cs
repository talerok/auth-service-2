using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class IdentitySourceLinkConfiguration : IEntityTypeConfiguration<IdentitySourceLink>
{
    public void Configure(EntityTypeBuilder<IdentitySourceLink> builder)
    {
        builder.ToTable("identity_source_links");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.IdentitySourceId).IsRequired();
        builder.Property(x => x.ExternalIdentity).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.IdentitySourceId, x.ExternalIdentity }).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasOne<IdentitySource>()
            .WithMany(x => x.Links)
            .HasForeignKey(x => x.IdentitySourceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
