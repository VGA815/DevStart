using DevStart.Domain.ConsentDocuments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.ConsentDocuments
{
    internal sealed class ConsentDocumentConfiguration : IEntityTypeConfiguration<ConsentDocument>
    {
        public void Configure(EntityTypeBuilder<ConsentDocument> builder)
        {
            builder.ToTable("consent_documents");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("type");
            builder.Property(x => x.Version)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("version");
            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("title");
            builder.Property(x => x.Content)
                .IsRequired()
                .HasColumnType("text")
                .HasColumnName("content");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(false);

            // One unique version per type
            builder.HasIndex(x => new { x.Type, x.Version })
                .IsUnique()
                .HasDatabaseName("ix_consent_documents_type_version");

            // Only one active document per type (partial unique index)
            builder.HasIndex(x => x.Type)
                .HasFilter("is_active = true")
                .IsUnique()
                .HasDatabaseName("ix_consent_documents_type_active");
        }
    }
}
