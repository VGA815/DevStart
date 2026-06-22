using DevStart.Domain.PromoCodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.PromoCodes
{
    internal sealed class PromoCodeRedemptionConfiguration : IEntityTypeConfiguration<PromoCodeRedemption>
    {
        public void Configure(EntityTypeBuilder<PromoCodeRedemption> builder)
        {
            builder.ToTable("promo_code_redemptions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.PromoCodeId).HasColumnName("promo_code_id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.PaymentId).HasColumnName("payment_id");
            builder.Property(x => x.SubscriptionId).HasColumnName("subscription_id");
            builder.Property(x => x.DiscountApplied)
                .HasColumnName("discount_applied")
                .HasColumnType("numeric(10,2)");
            builder.Property(x => x.RedeemedAt).HasColumnName("redeemed_at");

            // Once-per-user enforcement.
            builder.HasIndex(x => new { x.PromoCodeId, x.UserId })
                .IsUnique()
                .HasDatabaseName("ux_promo_code_redemptions_promo_user");

            builder.HasOne<PromoCode>()
                .WithMany()
                .HasForeignKey(x => x.PromoCodeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
