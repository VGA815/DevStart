using DevStart.Domain.Startups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Startups
{
    internal sealed class StartupValuationSnapshotConfiguration : IEntityTypeConfiguration<StartupValuationSnapshot>
    {
        public void Configure(EntityTypeBuilder<StartupValuationSnapshot> builder)
        {
            builder.ToTable("startup_valuation_snapshots");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");

            builder.Property(x => x.TotalScore).HasColumnName("total_score").HasColumnType("numeric(6,2)");
            builder.Property(x => x.TeamScore).HasColumnName("team_score").HasColumnType("numeric(6,2)");
            builder.Property(x => x.MarketScore).HasColumnName("market_score").HasColumnType("numeric(6,2)");
            builder.Property(x => x.ProductScore).HasColumnName("product_score").HasColumnType("numeric(6,2)");
            builder.Property(x => x.TractionScore).HasColumnName("traction_score").HasColumnType("numeric(6,2)");
            builder.Property(x => x.CompetitionScore).HasColumnName("competition_score").HasColumnType("numeric(6,2)");

            builder.Property(x => x.ValuationLow).HasColumnName("valuation_low").HasColumnType("numeric(18,2)");
            builder.Property(x => x.ValuationHigh).HasColumnName("valuation_high").HasColumnType("numeric(18,2)");
            builder.Property(x => x.ValuationPoint).HasColumnName("valuation_point").HasColumnType("numeric(18,2)");

            builder.Property(x => x.MethodsUsed).HasColumnName("methods_used").HasColumnType("text").IsRequired();
            builder.Property(x => x.BreakdownJson).HasColumnName("breakdown_json").HasColumnType("jsonb");
            builder.Property(x => x.MethodologyVersion).HasColumnName("methodology_version").IsRequired();
            builder.Property(x => x.CalculatedAt).HasColumnName("calculated_at");

            builder.HasIndex(x => new { x.StartupId, x.CalculatedAt })
                .HasDatabaseName("ix_startup_valuation_snapshots_startup_calculated");
        }
    }
}
