using DevStart.Domain.PatentRegistry;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.PatentRegistry
{
    internal sealed class PatentRegistryEntryConfiguration : IEntityTypeConfiguration<PatentRegistryEntry>
    {
        public void Configure(EntityTypeBuilder<PatentRegistryEntry> builder)
        {
            builder.ToTable("patent_registry_entries");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.Kind).HasColumnName("kind");
            builder.Property(x => x.NumberNormalized).HasColumnName("number_normalized").HasMaxLength(20).IsRequired();
            builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(1000);
            builder.Property(x => x.HolderName).HasColumnName("holder_name").HasMaxLength(500);
            builder.Property(x => x.HolderInn).HasColumnName("holder_inn").HasMaxLength(12);
            builder.Property(x => x.RegisteredOn).HasColumnName("registered_on");
            builder.Property(x => x.Status).HasColumnName("status");
            builder.Property(x => x.SourceNote).HasColumnName("source_note").HasMaxLength(500);
            builder.Property(x => x.FetchedAt).HasColumnName("fetched_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            // The upsert key and the resolve lookup are the same pair, so one unique index serves both.
            builder.HasIndex(x => new { x.Kind, x.NumberNormalized })
                .IsUnique()
                .HasDatabaseName("ux_patent_registry_entries_kind_number");

            // Reverse lookup: "which records name this INN as rightsholder" (SC-66). Partial, because
            // open data does not always carry an INN and a null one is never searched for.
            builder.HasIndex(x => x.HolderInn)
                .HasFilter("holder_inn IS NOT NULL")
                .HasDatabaseName("ix_patent_registry_entries_holder_inn");
        }
    }
}
