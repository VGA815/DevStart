using DevStart.Domain.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Admin
{
    internal sealed class AdminActionLogConfiguration : IEntityTypeConfiguration<AdminActionLog>
    {
        public void Configure(EntityTypeBuilder<AdminActionLog> builder)
        {
            builder.ToTable("admin_action_logs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.AdminUserId).HasColumnName("admin_user_id");
            builder.Property(x => x.ActionType)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("action_type");
            builder.Property(x => x.TargetType)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("target_type");
            builder.Property(x => x.TargetId).HasColumnName("target_id");
            builder.Property(x => x.Reason)
                .HasColumnName("reason")
                .HasColumnType("text")
                .IsRequired();
            builder.Property(x => x.MetadataJson)
                .HasColumnName("metadata_json")
                .HasColumnType("jsonb");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            builder.HasIndex(x => new { x.TargetType, x.TargetId })
                .HasDatabaseName("ix_admin_action_logs_target");
            builder.HasIndex(x => x.AdminUserId)
                .HasDatabaseName("ix_admin_action_logs_admin_user_id");
            builder.HasIndex(x => x.CreatedAt)
                .HasDatabaseName("ix_admin_action_logs_created_at");
        }
    }
}
