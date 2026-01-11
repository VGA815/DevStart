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
            builder.Property(x => x.Name).HasColumnName("name");
            builder.Property(x => x.FileUrl).HasColumnName("file_url");
            builder.Property(x => x.FileType).HasColumnName("file_type");
            builder.Property(x => x.UploadDate).HasColumnName("upload_date");
        }
    }
}
