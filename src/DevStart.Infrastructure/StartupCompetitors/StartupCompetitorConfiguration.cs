using DevStart.Domain.StartupCompetitors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.StartupCompetitors
{
    internal sealed class StartupCompetitorConfiguration : IEntityTypeConfiguration<StartupCompetitor>
    {
        public void Configure(EntityTypeBuilder<StartupCompetitor> builder)
        {
            builder.ToTable("startup_competitors");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            builder.Property(x => x.Website).HasColumnName("website").HasMaxLength(2000);
            builder.Property(x => x.NormalizedDomain).HasColumnName("normalized_domain").HasMaxLength(253);
            builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
            builder.Property(x => x.StrengthsVsUs).HasColumnName("strengths_vs_us").HasMaxLength(2000);
            builder.Property(x => x.WeaknessesVsUs).HasColumnName("weaknesses_vs_us").HasMaxLength(2000);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.StartupId);

            // One card per domain within a startup. Nulls stay distinct on purpose: rows created
            // before the website became mandatory have no domain and must not collide with each other.
            builder.HasIndex(x => new { x.StartupId, x.NormalizedDomain })
                .IsUnique()
                .HasDatabaseName("ux_startup_competitors_startup_domain");
        }
    }
}
