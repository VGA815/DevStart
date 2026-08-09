using DevStart.Domain.StartupDocumentFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.StartupDocumentFiles
{
    internal sealed class StartupDocumentFileConfiguration : IEntityTypeConfiguration<StartupDocumentFile>
    {
        public void Configure(EntityTypeBuilder<StartupDocumentFile> builder)
        {
            builder.ToTable("startup_document_files");

            builder.HasKey(x => x.Id);

            // The table had no index beyond the primary key, yet is only ever read by owner:
            // per-startup (document listings, the pitch-deck check in the community-standards
            // sweep) and per-uploader.
            builder.HasIndex(x => x.StartupId)
                .HasDatabaseName("ix_startup_document_files_startup");
            builder.HasIndex(x => x.UploaderId)
                .HasDatabaseName("ix_startup_document_files_uploader");

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.ObjectName).HasColumnName("object_name");
            builder.Property(x => x.Bucket).HasColumnName("bucket");
            builder.Property(x => x.DocumentName).HasColumnName("document_name");
            builder.Property(x => x.UploadDate).HasColumnName("upload_date");
            builder.Property(x => x.DocumentType).HasColumnName("document_type");
            builder.Property(x => x.FileSize).HasColumnName("file_size");
            builder.Property(x => x.UploaderId).HasColumnName("uploader_id");
        }
    }
}
