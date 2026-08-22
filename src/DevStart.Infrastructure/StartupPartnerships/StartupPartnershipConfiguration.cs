using DevStart.Domain.StartupPartnerships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.StartupPartnerships
{
    internal sealed class StartupPartnershipConfiguration : IEntityTypeConfiguration<StartupPartnership>
    {
        public void Configure(EntityTypeBuilder<StartupPartnership> builder)
        {
            builder.ToTable("startup_partnerships");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.PartnerName).HasColumnName("partner_name").HasMaxLength(200).IsRequired();
            builder.Property(x => x.Website).HasColumnName("website").HasMaxLength(2000).IsRequired();
            builder.Property(x => x.NormalizedDomain)
                .HasColumnName("normalized_domain").HasMaxLength(253).IsRequired();
            builder.Property(x => x.Kind).HasColumnName("kind");
            builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            // Computed from Description on read — never stored, so it cannot drift from the column.
            builder.Ignore(x => x.IsWorkedOut);

            // One record per partner domain within a startup — the race-safe backstop behind the 409
            // the handler returns, and what stops one partner filling the Berkus ceiling three times.
            // Unlike the competitor cards there are no legacy rows without a domain, so the column is
            // NOT NULL and the index needs no nulls-distinct caveat. No standalone startup_id index:
            // it is a left prefix of this one.
            builder.HasIndex(x => new { x.StartupId, x.NormalizedDomain })
                .IsUnique()
                .HasDatabaseName("ux_startup_partnerships_startup_domain");
        }
    }
}
