using DevStart.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Notifications
{
    internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("notifications");
            builder.HasKey(n => n.Id);
            builder.Property(n => n.UserId).IsRequired().HasColumnName("user_id");
            builder.Property(n => n.Type)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("type");
            builder.Property(n => n.Title).HasMaxLength(200).IsRequired().HasColumnName("title");
            builder.Property(n => n.Body).HasMaxLength(1000).IsRequired().HasColumnName("body");
            builder.HasIndex(n => new { n.UserId, n.IsRead });
            builder.HasIndex(n => n.CreatedAt);
        }
    }
}
