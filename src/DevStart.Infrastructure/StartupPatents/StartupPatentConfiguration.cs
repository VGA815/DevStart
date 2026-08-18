using DevStart.Domain.StartupPatents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.StartupPatents
{
    internal sealed class StartupPatentConfiguration : IEntityTypeConfiguration<StartupPatent>
    {
        public void Configure(EntityTypeBuilder<StartupPatent> builder)
        {
            builder.ToTable("startup_patents");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.Kind).HasColumnName("kind");
            builder.Property(x => x.NumberRaw).HasColumnName("number_raw").HasMaxLength(100).IsRequired();
            builder.Property(x => x.NumberNormalized).HasColumnName("number_normalized").HasMaxLength(20).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            // One record per (kind, number) within a startup — the race-safe backstop behind the 409
            // the handler returns. No standalone startup_id index: it is a left prefix of this one.
            builder.HasIndex(x => new { x.StartupId, x.Kind, x.NumberNormalized })
                .IsUnique()
                .HasDatabaseName("ux_startup_patents_startup_kind_number");
        }
    }
}
