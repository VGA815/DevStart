using DevStart.Domain.DealDocuments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.DealDocuments
{
    internal sealed class DealDocumentConfiguration : IEntityTypeConfiguration<DealDocument>
    {
        public void Configure(EntityTypeBuilder<DealDocument> builder)
        {
            builder.ToTable("deal_documents");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.DealId).HasColumnName("deal_id");
            builder.Property(x => x.TermSheetObjectKey)
                .HasColumnName("term_sheet_object_key")
                .HasMaxLength(500)
                .IsRequired();
            builder.Property(x => x.TermSheetPdfObjectKey)
                .HasColumnName("term_sheet_pdf_object_key")
                .HasMaxLength(500)
                .IsRequired();
            builder.Property(x => x.TermSheetPdfSha256)
                .HasColumnName("term_sheet_pdf_sha256")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(x => x.CapTableObjectKey)
                .HasColumnName("cap_table_object_key")
                .HasMaxLength(500)
                .IsRequired();
            builder.Property(x => x.GeneratedAt).HasColumnName("generated_at");

            builder.HasIndex(x => x.DealId).IsUnique().HasDatabaseName("ix_deal_documents_deal_id");
        }
    }
}
