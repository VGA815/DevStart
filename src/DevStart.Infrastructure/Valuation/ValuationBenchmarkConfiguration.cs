using DevStart.Domain.Valuation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Valuation
{
    internal sealed class ValuationBenchmarkConfiguration : IEntityTypeConfiguration<ValuationBenchmark>
    {
        public void Configure(EntityTypeBuilder<ValuationBenchmark> builder)
        {
            builder.ToTable("valuation_benchmark");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.MetricType)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("metric_type");

            builder.Property(x => x.Industry)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("industry");

            // Nullable for revenue multiples (sector-only); set for medians.
            builder.Property(x => x.Stage)
                .HasConversion<int?>()
                .HasColumnName("stage");

            builder.Property(x => x.Value)
                .HasColumnName("value")
                .HasColumnType("numeric(18,2)");

            builder.Property(x => x.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3);

            builder.Property(x => x.EffectiveFrom).HasColumnName("effective_from");

            builder.Property(x => x.Source)
                .HasColumnName("source")
                .HasMaxLength(512)
                .IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");

            // One row per (metric, sector, stage, version). NULLS NOT DISTINCT so multiplier rows
            // (stage = NULL) are deduplicated by the unique index just like medians (PostgreSQL 15+).
            builder.HasIndex(x => new { x.MetricType, x.Industry, x.Stage, x.EffectiveFrom })
                .IsUnique()
                .AreNullsDistinct(false)
                .HasDatabaseName("ux_valuation_benchmark_metric_industry_stage_effective");
        }
    }
}
