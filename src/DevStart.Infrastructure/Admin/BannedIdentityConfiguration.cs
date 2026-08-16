using DevStart.Domain.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Admin
{
    internal sealed class BannedIdentityConfiguration : IEntityTypeConfiguration<BannedIdentity>
    {
        public void Configure(EntityTypeBuilder<BannedIdentity> builder)
        {
            builder.ToTable("banned_identities");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.EmailHash)
                .HasColumnName("email_hash")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(x => x.BanExpiresAt).HasColumnName("ban_expires_at");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            // Registration reads this on every attempt, by hash alone.
            builder.HasIndex(x => x.EmailHash)
                .HasDatabaseName("ix_banned_identities_email_hash");
        }
    }
}
