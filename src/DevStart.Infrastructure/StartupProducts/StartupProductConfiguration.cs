using DevStart.Domain.StartupProducts;
using DevStart.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.StartupProducts
{
    internal sealed class StartupProductConfiguration : IEntityTypeConfiguration<StartupProduct>
    {
        public void Configure(EntityTypeBuilder<StartupProduct> builder)
        {
            builder.ToTable("startup_products");

            builder.HasKey(x => x.StartupId);

            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.Problem).HasColumnName("problem");
            builder.Property(x => x.Solution).HasColumnName("solution");
            builder.Property(x => x.ValueProposition).HasColumnName("value_proposition");
            builder.Property(x => x.Differentiators).HasColumnName("differentiators");

            builder.Property(x => x.Stack)
                .HasColumnName("stack")
                .HasConversion(EfJson.StringListConverter)
                .HasColumnType("jsonb");
        }
    }
}
