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
            builder.Property(x => x.ServiceOrderId).HasColumnName("service_order_id");
            builder.Property(x => x.Purpose)
                .HasConversion<int>()
                .IsRequired()
                .HasDefaultValue(DevStart.Domain.Payments.PaymentPurpose.Subscription)
                .HasColumnName("purpose");
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
            builder.Property(x => x.PromoCodeId).HasColumnName("promo_code_id");
            builder.Property(x => x.DiscountAmount)
                .HasColumnName("discount_amount")
                .HasColumnType("numeric(10,2)")
                .HasDefaultValue(0m);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.PaidAt).HasColumnName("paid_at");

            builder.HasIndex(x => x.SubscriptionId).HasDatabaseName("ix_payments_subscription_id");
            builder.HasIndex(x => x.ServiceOrderId).HasDatabaseName("ix_payments_service_order_id");
            builder.HasIndex(x => new { x.Provider, x.ProviderPaymentId })
                .IsUnique()
                .HasFilter("provider_payment_id IS NOT NULL")
                .HasDatabaseName("ix_payments_provider_payment");
            builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_payments_user_status");
            builder.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("ix_payments_status_created");

            // At most one in-flight (Pending = 0) *subscription* (purpose = 0) payment per user, so a
            // concurrent/double-clicked subscription checkout cannot create a second payment and charge
            // the user twice. One-time service orders (purpose = 1) are not constrained — each is a
            // distinct purchase.
            builder.HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("status = 0 AND purpose = 0")
                .HasDatabaseName("ux_payments_user_pending");

            builder.HasOne<DevStart.Domain.Subscriptions.Subscription>()
                .WithMany()
                .HasForeignKey(x => x.SubscriptionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<DevStart.Domain.ServiceOrders.ServiceOrder>()
                .WithMany()
                .HasForeignKey(x => x.ServiceOrderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.PromoCodeId).HasDatabaseName("ix_payments_promo_code_id");

            // Restrict: promo codes are deactivated, never deleted, so a payment's promo reference stays valid.
            builder.HasOne<DevStart.Domain.PromoCodes.PromoCode>()
                .WithMany()
                .HasForeignKey(x => x.PromoCodeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
