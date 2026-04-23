using DevStart.Domain.Messages;
using DevStart.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Messages
{
    internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("messages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.SenderId).HasColumnName("sender_id");
            builder.Property(x => x.SenderType)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("sender_type");
            builder.Property(x => x.ReceiverId).HasColumnName("receiver_id");
            builder.Property(x => x.ReceiverType)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("receiver_type");
            builder.Property(x => x.TextContent).HasColumnName("text_content");
            builder.Property(x => x.IsRead).HasColumnName("is_read");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.Property(x => x.MediaIds)
                .HasColumnName("media_ids")
                .HasColumnType("jsonb")
                .HasConversion(EfJson.GuidListConverter);

            builder.Property(x => x.MetricIds)
                .HasColumnName("metric_ids")
                .HasColumnType("jsonb")
                .HasConversion(EfJson.GuidListConverter);

            builder.HasIndex(x => new { x.SenderId, x.ReceiverId }).HasDatabaseName("ix_messages_sender_receiver");
            builder.HasIndex(x => new { x.ReceiverId, x.IsRead }).HasDatabaseName("ix_messages_receiver_is_read");
            builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_messages_created_at");
        }
    }
}
