using DevStart.Domain.ServiceOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.ServiceOrders
{
    internal sealed class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
    {
        public void Configure(EntityTypeBuilder<ServiceOrder> builder)
        {
            builder.ToTable("service_orders");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.ServiceType)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("service_type");
            builder.Property(x => x.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(10,2)");
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
            builder.Property(x => x.FulfilledAt).HasColumnName("fulfilled_at");

            builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_service_orders_user_status");
        }
    }
}
