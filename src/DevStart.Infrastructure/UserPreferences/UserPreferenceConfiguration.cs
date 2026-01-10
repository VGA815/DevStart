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
            builder.HasKey(up => up.UserId);

            builder.HasOne<User>().WithOne();
        }
    }
}
