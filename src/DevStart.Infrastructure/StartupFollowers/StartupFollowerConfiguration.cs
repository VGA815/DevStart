using DevStart.Domain.StartupFollowers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.StartupFollowers
{
    internal sealed class StartupFollowerConfiguration : IEntityTypeConfiguration<StartupFollower>
    {
        public void Configure(EntityTypeBuilder<StartupFollower> builder)
        {
            builder.ToTable("startup_followers");

            builder.HasKey(x => new { x.ProfileId, x.StartupId });

            builder.Property(x => x.ProfileId).HasColumnName("profile_id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        }
    }
}
