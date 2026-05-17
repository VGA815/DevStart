using DevStart.Domain.ExternalLogins;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.ExternalLogins
{
    internal sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
    {
        public void Configure(EntityTypeBuilder<ExternalLogin> builder)
        {
            builder.ToTable("external_logins");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.Provider).HasColumnName("provider").HasConversion<int>();
            builder.Property(x => x.ProviderUserId).HasColumnName("provider_user_id").IsRequired().HasMaxLength(255);
            builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.LastUsedAt).HasColumnName("last_used_at");

            builder.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();
            builder.HasIndex(x => x.UserId);
        }
    }
}
