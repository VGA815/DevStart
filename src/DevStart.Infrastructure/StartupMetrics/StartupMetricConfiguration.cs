using DevStart.Domain.StartupMetrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.StartupMetrics
{
    internal sealed class StartupMetricConfiguration : IEntityTypeConfiguration<StartupMetric>
    {
        public void Configure(EntityTypeBuilder<StartupMetric> builder)
        {
            builder.ToTable("startup_metrics");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.MetricType).HasColumnName("metric_type");
            builder.Property(x => x.Value).HasColumnName("value");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            builder.HasIndex(x => x.StartupId);
        }
    }
}
