using DevStart.Domain.TwoFactor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.TwoFactor
{
    internal sealed class TwoFactorRecoveryCodeConfiguration : IEntityTypeConfiguration<TwoFactorRecoveryCode>
    {
        public void Configure(EntityTypeBuilder<TwoFactorRecoveryCode> builder)
        {
            builder.ToTable("two_factor_recovery_codes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.CodeHash).HasColumnName("code_hash").IsRequired().HasMaxLength(64);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UsedAt).HasColumnName("used_at");

            builder.Ignore(x => x.IsUsed);

            builder.HasIndex(x => new { x.UserId, x.CodeHash }).IsUnique();
        }
    }
}
