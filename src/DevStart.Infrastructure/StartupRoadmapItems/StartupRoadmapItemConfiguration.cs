using DevStart.Domain.StartupRoadmapItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.StartupRoadmapItems
{
    internal sealed class StartupRoadmapItemConfiguration : IEntityTypeConfiguration<StartupRoadmapItem>
    {
        public void Configure(EntityTypeBuilder<StartupRoadmapItem> builder)
        {
            builder.ToTable("startup_roadmap_items");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.StartupStage).HasColumnName("startup_stage");
            builder.Property(x => x.Title).HasColumnName("title").IsRequired();
            builder.Property(x => x.Desription).HasColumnName("description");
            builder.Property(x => x.Status).HasColumnName("status");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.TargetDate).HasColumnName("target_date");

            builder.HasIndex(x => x.StartupId);
        }
    }
}
