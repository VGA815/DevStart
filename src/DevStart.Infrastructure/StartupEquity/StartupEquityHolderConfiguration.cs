using DevStart.Domain.StartupEquity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.StartupEquity
{
    internal sealed class StartupEquityHolderConfiguration : IEntityTypeConfiguration<StartupEquityHolder>
    {
        public void Configure(EntityTypeBuilder<StartupEquityHolder> builder)
        {
            builder.ToTable("startup_equity_holders");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.ProfileId).HasColumnName("profile_id");

            builder.Property(x => x.HolderType)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("holder_type");

            builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(200);

            builder.Property(x => x.EquityPercentage)
                .HasColumnName("equity_percentage")
                .HasColumnType("numeric(5,2)");

            builder.Property(x => x.VestingStartDate).HasColumnName("vesting_start_date");
            builder.Property(x => x.VestingMonths).HasColumnName("vesting_months");
            builder.Property(x => x.CliffMonths).HasColumnName("cliff_months");

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.StartupId).HasDatabaseName("ix_startup_equity_holders_startup_id");
        }
    }
}
