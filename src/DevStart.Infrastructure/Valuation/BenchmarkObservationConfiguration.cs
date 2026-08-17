using DevStart.Domain.Valuation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Valuation
{
    internal sealed class BenchmarkObservationConfiguration : IEntityTypeConfiguration<BenchmarkObservation>
    {
        public void Configure(EntityTypeBuilder<BenchmarkObservation> builder)
        {
            builder.ToTable("benchmark_observation");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.Source)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("source");

            builder.Property(x => x.IssuerId).HasColumnName("issuer_id");

            builder.Property(x => x.ExternalKey)
                .HasColumnName("external_key")
                .HasMaxLength(200);

            builder.Property(x => x.Metric)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("metric");

            // Wide enough for a market capitalisation in roubles — hundreds of billions, kopecks kept.
            builder.Property(x => x.Value)
                .HasColumnName("value")
                .HasColumnType("numeric(24,4)");

            builder.Property(x => x.AsOf).HasColumnName("as_of");
            builder.Property(x => x.FiscalYear).HasColumnName("fiscal_year");

            builder.Property(x => x.DatasetRegion)
                .HasColumnName("dataset_region")
                .HasMaxLength(64);

            builder.Property(x => x.FetchedAt).HasColumnName("fetched_at");

            builder.Property(x => x.OriginNote)
                .HasColumnName("origin_note")
                .HasMaxLength(512);

            // Deactivating an issuer must not delete the observations already collected under it, and
            // deleting an issuer outright is not something the admin API offers — Restrict makes both
            // explicit rather than leaving the default cascade to quietly erase collected facts.
            builder.HasOne<BenchmarkIssuer>()
                .WithMany()
                .HasForeignKey(x => x.IssuerId)
                .OnDelete(DeleteBehavior.Restrict);

            // The staging upsert key. NULLS NOT DISTINCT so issuer rows (external_key and
            // dataset_region NULL) and bucket rows (issuer_id NULL) are both deduplicated by this one
            // index (PostgreSQL 15+).
            //
            // dataset_region is part of the key because the derivation selects a slice by name: without
            // it, staging two Damodaran regions for the same year would silently overwrite one with the
            // other, and the survivor would be indistinguishable from a complete set.
            builder.HasIndex(x => new { x.Source, x.IssuerId, x.ExternalKey, x.DatasetRegion, x.Metric, x.AsOf })
                .IsUnique()
                .AreNullsDistinct(false)
                .HasDatabaseName("ux_benchmark_observation_key");

            // The derivation reads "everything of this metric at or before the valuation quarter".
            builder.HasIndex(x => new { x.Metric, x.AsOf })
                .HasDatabaseName("ix_benchmark_observation_metric_as_of");
        }
    }
}
