using DevStart.Domain.TrustedDevices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.TrustedDevices
{
    internal sealed class TrustedDeviceConfiguration : IEntityTypeConfiguration<TrustedDevice>
    {
        public void Configure(EntityTypeBuilder<TrustedDevice> builder)
        {
            builder.ToTable("trusted_devices");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.TokenHash).HasColumnName("token_hash").IsRequired().HasMaxLength(128);
            builder.Property(x => x.Label).HasColumnName("label").HasMaxLength(128);
            builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
            builder.Property(x => x.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(64);
            builder.Property(x => x.LastSeenIp).HasColumnName("last_seen_ip").HasMaxLength(64);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            builder.Property(x => x.LastUsedAt).HasColumnName("last_used_at");
            builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");

            builder.Ignore(x => x.IsRevoked);

            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.ExpiresAt);
        }
    }
}
