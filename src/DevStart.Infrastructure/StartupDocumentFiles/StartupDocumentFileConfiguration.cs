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
