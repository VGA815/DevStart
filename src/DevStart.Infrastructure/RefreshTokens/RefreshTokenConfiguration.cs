using DevStart.Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.RefreshTokens
{
    internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.TokenHash).HasColumnName("token_hash").IsRequired().HasMaxLength(128);
            builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            builder.Property(x => x.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
            builder.Property(x => x.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(64);
            builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(512);

            builder.Ignore(x => x.IsRevoked);

            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.ExpiresAt);
        }
    }
}
