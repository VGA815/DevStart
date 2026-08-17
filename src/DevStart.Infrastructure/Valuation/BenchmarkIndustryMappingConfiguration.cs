using DevStart.Domain.Valuation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Valuation
{
    internal sealed class BenchmarkIndustryMappingConfiguration : IEntityTypeConfiguration<BenchmarkIndustryMapping>
    {
        public void Configure(EntityTypeBuilder<BenchmarkIndustryMapping> builder)
        {
            builder.ToTable("benchmark_industry_mapping");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.SourceKind)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("source_kind");

            builder.Property(x => x.ExternalKey)
                .HasColumnName("external_key")
                .HasMaxLength(200)
                .IsRequired();

            // Nullable: "deliberately not mapped" is an answer we store, not an absence of a row.
            builder.Property(x => x.Industry)
                .HasConversion<int?>()
                .HasColumnName("industry");

            builder.Property(x => x.Note)
                .HasColumnName("note")
                .HasMaxLength(512);

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            // The natural key is (source_kind, external_key) compared case-insensitively: readers build
            // lookup dictionaries with OrdinalIgnoreCase, so two rows differing only in casing would
            // make every read throw on a duplicate key. A plain unique index cannot express that —
            // PostgreSQL compares text case-sensitively — so the constraint is a functional unique
            // index on lower(external_key), created by raw SQL in the migration that adds this table.
            // It is deliberately absent from the fluent model: EF Core has no expression-index API, and
            // declaring a case-sensitive index here as well would only add a weaker duplicate.
            // Writers must look the row up through lower(...) to match it; see
            // SaveBenchmarkIndustryMappingCommandHandler.
        }
    }
}
