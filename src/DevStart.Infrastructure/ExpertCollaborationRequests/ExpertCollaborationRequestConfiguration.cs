using DevStart.Domain.ExpertCollaborationRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.ExpertCollaborationRequests
{
    internal sealed class ExpertCollaborationRequestConfiguration : IEntityTypeConfiguration<ExpertCollaborationRequest>
    {
        public void Configure(EntityTypeBuilder<ExpertCollaborationRequest> builder)
        {
            builder.ToTable("expert_collaboration_requests");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.ExpertProfileId).HasColumnName("expert_profile_id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.CollaborationType)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("collaboration_type");
            builder.Property(x => x.Message).HasMaxLength(2000).HasColumnName("message");
            builder.Property(x => x.ProposedHoursPerWeek).HasColumnName("proposed_hours_per_week");
            builder.Property(x => x.ProposedRate).HasColumnName("proposed_rate").HasColumnType("numeric(18,2)");
            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("status");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => new { x.StartupId, x.Status })
                .HasDatabaseName("ix_expert_collaboration_requests_startup_status");
            builder.HasIndex(x => new { x.ExpertProfileId, x.Status })
                .HasDatabaseName("ix_expert_collaboration_requests_expert_status");
            builder.HasIndex(x => new { x.ExpertProfileId, x.StartupId })
                .IsUnique()
                .HasFilter("status = 0")
                .HasDatabaseName("ux_expert_collaboration_requests_expert_startup_pending");
        }
    }
}
