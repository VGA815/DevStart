using DevStart.SharedKernel;

namespace DevStart.Domain.ExpertCollaborationRequests
{
    public sealed class ExpertCollaborationRequest : Entity
    {
        public Guid Id { get; set; }
        public Guid ExpertProfileId { get; set; }
        public Guid StartupId { get; set; }
        public CollaborationType CollaborationType { get; set; }
        public string? Message { get; set; }
        public int? ProposedHoursPerWeek { get; set; }
        public decimal? ProposedRate { get; set; }
        public ExpertCollaborationRequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ExpertCollaborationRequest()
        {
        }

        public static ExpertCollaborationRequest Create(
            Guid expertProfileId,
            Guid startupId,
            CollaborationType collaborationType,
            string? message,
            int? proposedHoursPerWeek,
            decimal? proposedRate,
            DateTime createdAt)
            => new()
            {
                Id = Guid.NewGuid(),
                ExpertProfileId = expertProfileId,
                StartupId = startupId,
                CollaborationType = collaborationType,
                Message = message,
                ProposedHoursPerWeek = proposedHoursPerWeek,
                ProposedRate = proposedRate,
                Status = ExpertCollaborationRequestStatus.Pending,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

        public Result Accept(DateTime utcNow)
        {
            if (Status != ExpertCollaborationRequestStatus.Pending)
            {
                return Result.Failure(ExpertCollaborationRequestErrors.MustBePending);
            }

            Status = ExpertCollaborationRequestStatus.Accepted;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        public Result Reject(DateTime utcNow)
        {
            if (Status != ExpertCollaborationRequestStatus.Pending)
            {
                return Result.Failure(ExpertCollaborationRequestErrors.MustBePending);
            }

            Status = ExpertCollaborationRequestStatus.Rejected;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        public Result Withdraw(DateTime utcNow)
        {
            if (Status != ExpertCollaborationRequestStatus.Pending)
            {
                return Result.Failure(ExpertCollaborationRequestErrors.MustBePending);
            }

            Status = ExpertCollaborationRequestStatus.Withdrawn;
            UpdatedAt = utcNow;
            return Result.Success();
        }
    }
}
