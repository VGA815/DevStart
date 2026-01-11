using DevStart.Domain.MediaFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.MediaFiles
{
    internal sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
    {
        public void Configure(EntityTypeBuilder<MediaFile> builder)
        {
            builder.ToTable("media_files");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UploaderId).HasColumnName("uploader_id");
            builder.Property(x => x.FileUrl).HasColumnName("file_url");
            builder.Property(x => x.Name).HasColumnName("name");
            builder.Property(x => x.FileType).HasColumnName("file_type");
            builder.Property(x => x.FileSize).HasColumnName("file_size");
            builder.Property(x => x.UploadDate).HasColumnName("upload_date");
        }
    }
}
