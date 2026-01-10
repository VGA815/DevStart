using DevStart.Domain.StartupRoadmapItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.StartupRoadmapItems
{
    internal sealed class StartupRoadmapItemConfiguration : IEntityTypeConfiguration<StartupRoadmapItem>
    {
        public void Configure(EntityTypeBuilder<StartupRoadmapItem> builder)
        {
            builder.HasKey(x => x);
        }
    }
}
