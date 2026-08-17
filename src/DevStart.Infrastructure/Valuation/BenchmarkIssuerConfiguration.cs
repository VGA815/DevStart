using DevStart.Domain.Valuation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Valuation
{
    internal sealed class BenchmarkIssuerConfiguration : IEntityTypeConfiguration<BenchmarkIssuer>
    {
        public void Configure(EntityTypeBuilder<BenchmarkIssuer> builder)
        {
            builder.ToTable("benchmark_issuer");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.Ticker)
                .HasColumnName("ticker")
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(x => x.Inn)
                .HasColumnName("inn")
                .HasMaxLength(12);

            builder.Property(x => x.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Industry)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("industry");

            builder.Property(x => x.IsActive).HasColumnName("is_active");

            builder.Property(x => x.RevenueOverride)
                .HasColumnName("revenue_override")
                .HasColumnType("numeric(20,2)");

            builder.Property(x => x.RevenueOverrideFiscalYear).HasColumnName("revenue_override_fiscal_year");

            builder.Property(x => x.RevenueOverrideNote)
                .HasColumnName("revenue_override_note")
                .HasMaxLength(512);

            builder.Property(x => x.Note)
                .HasColumnName("note")
                .HasMaxLength(512);

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.Ticker)
                .IsUnique()
                .HasDatabaseName("ux_benchmark_issuer_ticker");

            // The collection jobs and the derivation both scan by sector among active issuers.
            builder.HasIndex(x => new { x.Industry, x.IsActive })
                .HasDatabaseName("ix_benchmark_issuer_industry_active");
        }
    }
}
