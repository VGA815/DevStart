using DevStart.Domain.UserPreferences;
using DevStart.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.UserPreferences
{
    internal sealed class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
    {
        public void Configure(EntityTypeBuilder<UserPreference> builder)
        {
            builder.ToTable("user_preferences");

            builder.HasKey(x => x.UserId);

            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.Theme).HasColumnName("theme");
            builder.Property(x => x.ReceiveNotifications).HasColumnName("receive_notifications");
        }
    }
}
