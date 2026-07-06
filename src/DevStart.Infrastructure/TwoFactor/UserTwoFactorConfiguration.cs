using DevStart.Domain.TwoFactor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.TwoFactor
{
    internal sealed class UserTwoFactorConfiguration : IEntityTypeConfiguration<UserTwoFactor>
    {
        public void Configure(EntityTypeBuilder<UserTwoFactor> builder)
        {
            builder.ToTable("user_two_factor");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.EncryptedSecret).HasColumnName("encrypted_secret").IsRequired().HasMaxLength(512);
            builder.Property(x => x.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(false);
            builder.Property(x => x.EnabledAt).HasColumnName("enabled_at");
            // Concurrency token: two concurrent submissions of the same TOTP code race on this
            // column — the loser gets a DbUpdateConcurrencyException instead of a token pair.
            builder.Property(x => x.LastUsedTimestep).HasColumnName("last_used_timestep").IsConcurrencyToken();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.UserId).IsUnique();
        }
    }
}
