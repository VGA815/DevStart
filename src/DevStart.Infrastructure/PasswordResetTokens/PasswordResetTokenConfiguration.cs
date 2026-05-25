using DevStart.Domain.PasswordResetTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.PasswordResetTokens
{
    internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.ToTable("password_reset_tokens");

            builder.HasKey(x => x.TokenId);

            builder.Property(x => x.TokenId).HasColumnName("token_id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");

            builder.HasIndex(x => x.UserId);
        }
    }
}
