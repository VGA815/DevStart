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

            // Serves the follower-count aggregate that orders the public catalogue, and the
            // "followers of this startup" listing. The primary key leads with profile_id and so
            // covers neither.
            builder.HasIndex(x => x.StartupId)
                .HasDatabaseName("ix_startup_followers_startup");

            builder.Property(x => x.ProfileId).HasColumnName("profile_id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        }
    }
}
