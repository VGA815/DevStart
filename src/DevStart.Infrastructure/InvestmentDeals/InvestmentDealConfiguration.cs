using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.InvestmentDeals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.InvestmentDeals
{
    internal sealed class InvestmentDealConfiguration : IEntityTypeConfiguration<InvestmentDeal>
    {
        public void Configure(EntityTypeBuilder<InvestmentDeal> builder)
        {
            builder.ToTable("investment_deals");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.ApplicationId).HasColumnName("application_id");
            builder.Property(x => x.InvestorProfileId).HasColumnName("investor_profile_id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.RoadmapItemId).HasColumnName("roadmap_item_id");
            builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
            builder.Property(x => x.ConfirmedByStartup).HasColumnName("confirmed_by_startup");
            builder.Property(x => x.ConfirmedByInvestor).HasColumnName("confirmed_by_investor");
            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("status");
            builder.Property(x => x.Instrument)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("instrument")
                .HasDefaultValue(InvestmentInstrument.Safe);
            builder.Property(x => x.ValuationCap).HasColumnName("valuation_cap").HasColumnType("numeric(18,2)");
            builder.Property(x => x.Discount).HasColumnName("discount").HasColumnType("numeric(5,4)");
            builder.Property(x => x.InterestRate).HasColumnName("interest_rate").HasColumnType("numeric(5,4)");
            builder.Property(x => x.TermMonths).HasColumnName("term_months");
            builder.Property(x => x.PreMoneyValuation).HasColumnName("pre_money_valuation").HasColumnType("numeric(18,2)");
            builder.Property(x => x.LiquidationPreference)
                .HasColumnName("liquidation_preference")
                .HasColumnType("numeric(5,2)")
                .HasDefaultValue(1.0m);
            builder.Property(x => x.ProRataRights).HasColumnName("pro_rata_rights").HasDefaultValue(false);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.CompletedAt).HasColumnName("completed_at");

            builder.HasIndex(x => x.ApplicationId).IsUnique().HasDatabaseName("ix_investment_deals_application_id");
            builder.HasIndex(x => new { x.StartupId, x.Status }).HasDatabaseName("ix_investment_deals_startup_status");
            builder.HasIndex(x => new { x.InvestorProfileId, x.Status }).HasDatabaseName("ix_investment_deals_investor_status");
        }
    }
}
