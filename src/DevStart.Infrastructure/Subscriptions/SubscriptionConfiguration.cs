using DevStart.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Subscriptions
{
    internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.ToTable("subscriptions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.Plan)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("plan");
            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("status");
            builder.Property(x => x.StartedAt).HasColumnName("started_at");
            builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.RenewalReminderSentAt).HasColumnName("renewal_reminder_sent_at");

            builder.HasIndex(x => new { x.UserId, x.Status })
                .HasDatabaseName("ix_subscriptions_user_status");
            builder.HasIndex(x => x.ExpiresAt)
                .HasDatabaseName("ix_subscriptions_expires_at");
            builder.HasIndex(x => new { x.Status, x.ExpiresAt })
                .HasDatabaseName("ix_subscriptions_status_expires");
        }
    }
}
