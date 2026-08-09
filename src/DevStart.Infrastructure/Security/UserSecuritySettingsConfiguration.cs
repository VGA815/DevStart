using DevStart.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Security
{
    internal sealed class UserSecuritySettingsConfiguration : IEntityTypeConfiguration<UserSecuritySettings>
    {
        public void Configure(EntityTypeBuilder<UserSecuritySettings> builder)
        {
            builder.ToTable("user_security_settings");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.Strictness).HasColumnName("strictness").HasConversion<int>();
            builder.Property(x => x.TrustDurationDays).HasColumnName("trust_duration_days");
            builder.Property(x => x.NotifyOnNewDeviceLogin).HasColumnName("notify_on_new_device_login");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.UserId).IsUnique();
        }
    }
}
