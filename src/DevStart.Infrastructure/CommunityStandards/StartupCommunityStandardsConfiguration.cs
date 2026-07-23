using DevStart.Domain.StartupCommunityStandards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.CommunityStandards
{
    internal sealed class StartupCommunityStandardsConfiguration : IEntityTypeConfiguration<StartupCommunityStandards>
    {
        public void Configure(EntityTypeBuilder<StartupCommunityStandards> builder)
        {
            builder.ToTable("startup_community_standards");

            // One row per startup: the projection is a replace-in-place summary, not a history.
            builder.HasKey(x => x.StartupId);

            builder.Property(x => x.StartupId).HasColumnName("startup_id").ValueGeneratedNever();
            builder.Property(x => x.CompletedCount).HasColumnName("completed_count");
            builder.Property(x => x.TotalCount).HasColumnName("total_count");

            builder.Property(x => x.Level)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("level");

            builder.Property(x => x.ComputedAt).HasColumnName("computed_at");

            // Supports the catalog's "at least this level" filter.
            builder.HasIndex(x => x.Level)
                .HasDatabaseName("ix_startup_community_standards_level");
        }
    }
}
