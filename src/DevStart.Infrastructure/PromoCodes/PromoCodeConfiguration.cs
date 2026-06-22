using DevStart.Domain.PromoCodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.PromoCodes
{
    internal sealed class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
    {
        public void Configure(EntityTypeBuilder<PromoCode> builder)
        {
            builder.ToTable("promo_codes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.Code)
                .HasColumnName("code")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(x => x.DiscountType)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("discount_type");
            builder.Property(x => x.DiscountValue)
                .HasColumnName("discount_value")
                .HasColumnType("numeric(10,2)");
            builder.Property(x => x.FreePeriodDays).HasColumnName("free_period_days");
            builder.Property(x => x.Plan)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("plan");
            builder.Property(x => x.MaxRedemptions).HasColumnName("max_redemptions");
            builder.Property(x => x.RedeemedCount).HasColumnName("redeemed_count");
            builder.Property(x => x.ValidFrom).HasColumnName("valid_from");
            builder.Property(x => x.ValidUntil).HasColumnName("valid_until");
            builder.Property(x => x.IsActive).HasColumnName("is_active");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");

            builder.HasIndex(x => x.Code)
                .IsUnique()
                .HasDatabaseName("ux_promo_codes_code");
        }
    }
}
