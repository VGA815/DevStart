using DevStart.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Payments
{
    internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("payments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.SubscriptionId).HasColumnName("subscription_id");
            builder.Property(x => x.Provider)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("provider");
            builder.Property(x => x.ProviderPaymentId)
                .HasColumnName("provider_payment_id")
                .HasMaxLength(200);
            builder.Property(x => x.ConfirmationUrl)
                .HasColumnName("confirmation_url")
                .HasMaxLength(2048);
            builder.Property(x => x.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(10,2)");
            builder.Property(x => x.RefundedAmount)
                .HasColumnName("refunded_amount")
                .HasColumnType("numeric(10,2)")
                .HasDefaultValue(0m);
            builder.Property(x => x.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("status");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.PaidAt).HasColumnName("paid_at");

            builder.HasIndex(x => x.SubscriptionId).HasDatabaseName("ix_payments_subscription_id");
            builder.HasIndex(x => new { x.Provider, x.ProviderPaymentId })
                .IsUnique()
                .HasFilter("provider_payment_id IS NOT NULL")
                .HasDatabaseName("ix_payments_provider_payment");
            builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_payments_user_status");
            builder.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("ix_payments_status_created");

            // At most one in-flight (Pending = 0) payment per user, so a concurrent/double-clicked
            // checkout cannot create a second payment and charge the user twice.
            builder.HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("status = 0")
                .HasDatabaseName("ux_payments_user_pending");

            builder.HasOne<DevStart.Domain.Subscriptions.Subscription>()
                .WithMany()
                .HasForeignKey(x => x.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
