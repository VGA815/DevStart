using DevStart.Domain.ChatFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.ChatFiles
{
    internal sealed class ChatFileConfiguration : IEntityTypeConfiguration<ChatFile>
    {
        public void Configure(EntityTypeBuilder<ChatFile> builder)
        {
            builder.ToTable("chat_files");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UploaderId).HasColumnName("uploader_id").IsRequired();
            builder.Property(x => x.MessageId).HasColumnName("message_id");
            builder.Property(x => x.ObjectName).HasColumnName("object_name").IsRequired();
            builder.Property(x => x.Bucket).HasColumnName("bucket").IsRequired();
            builder.Property(x => x.FileName)
                .HasColumnName("file_name")
                .HasMaxLength(ChatFileRules.MaxFileNameLength)
                .IsRequired();
            builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(255).IsRequired();
            builder.Property(x => x.FileSize).HasColumnName("file_size").IsRequired();
            builder.Property(x => x.UploadDate).HasColumnName("upload_date").IsRequired();

            builder.HasIndex(x => x.UploaderId).HasDatabaseName("ix_chat_files_uploader_id");
            builder.HasIndex(x => x.MessageId).HasDatabaseName("ix_chat_files_message_id");
        }
    }
}
