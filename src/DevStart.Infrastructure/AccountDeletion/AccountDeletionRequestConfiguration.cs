using DevStart.Domain.AccountDeletion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.AccountDeletion
{
    internal sealed class AccountDeletionRequestConfiguration : IEntityTypeConfiguration<AccountDeletionRequest>
    {
        public void Configure(EntityTypeBuilder<AccountDeletionRequest> builder)
        {
            builder.ToTable("account_deletion_requests");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.RequestedAt).HasColumnName("requested_at");
            builder.Property(x => x.ScheduledFor).HasColumnName("scheduled_for");
            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("status");
            builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
            builder.Property(x => x.CompletedAt).HasColumnName("completed_at");

            // One open request per user — the second click must fail on the way in, not produce a
            // duplicate the job would then try to erase twice.
            builder.HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("status = 0")
                .HasDatabaseName("ix_account_deletion_requests_user_pending");

            // The job's only query: pending rows whose window has closed.
            builder.HasIndex(x => new { x.Status, x.ScheduledFor })
                .HasDatabaseName("ix_account_deletion_requests_due");

            // No foreign key to users on purpose: a completed request outlives the row it points at,
            // and it is the record proving the erasure happened when it was promised.
        }
    }
}
