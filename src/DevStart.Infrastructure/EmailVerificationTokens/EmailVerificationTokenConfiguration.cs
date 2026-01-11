using DevStart.Domain.EmailVerificationTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.EmailVerificationTokens
{
    internal sealed class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
    {
        public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
        {
            builder.ToTable("email_verification_tokens");

            builder.HasKey(x => x.TokenId);

            builder.Property(x => x.TokenId).HasColumnName("token_id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");

            builder.HasIndex(x => x.UserId);
        }
    }
}
