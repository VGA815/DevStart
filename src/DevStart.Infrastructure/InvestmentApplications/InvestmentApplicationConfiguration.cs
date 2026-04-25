using DevStart.Domain.InvestmentApplications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.InvestmentApplications
{
    internal sealed class InvestmentApplicationConfiguration : IEntityTypeConfiguration<InvestmentApplication>
    {
        public void Configure(EntityTypeBuilder<InvestmentApplication> builder)
        {
            builder.ToTable("investment_applications");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.InvestorProfileId).HasColumnName("investor_profile_id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.RoadmapItemId).HasColumnName("roadmap_item_id");
            builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
            builder.Property(x => x.Message).HasMaxLength(2000).HasColumnName("message");
            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("status");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => new { x.StartupId, x.Status }).HasDatabaseName("ix_investment_applications_startup_status");
            builder.HasIndex(x => new { x.InvestorProfileId, x.Status }).HasDatabaseName("ix_investment_applications_investor_status");
            builder.HasIndex(x => x.RoadmapItemId).HasDatabaseName("ix_investment_applications_roadmap_item_id");
        }
    }
}
