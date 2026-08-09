using DevStart.Domain.StartupInvestors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.StartupInvestors
{
    internal sealed class StartupInvestorConfiguration : IEntityTypeConfiguration<StartupInvestor>
    {
        public void Configure(EntityTypeBuilder<StartupInvestor> builder)
        {
            builder.ToTable("startup_investors");

            builder.HasKey(x => new { x.ProfileId, x.StartupId });

            // Same shape as startup_members: the composite key leads with profile_id, so listing a
            // startup's investors would otherwise scan the table.
            builder.HasIndex(x => x.StartupId)
                .HasDatabaseName("ix_startup_investors_startup");

            builder.Property(x => x.ProfileId).HasColumnName("profile_id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.IsPublic).HasColumnName("is_public");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        }
    }
}
