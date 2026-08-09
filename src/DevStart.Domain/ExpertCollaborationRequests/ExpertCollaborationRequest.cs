using DevStart.SharedKernel;

namespace DevStart.Domain.ExpertCollaborationRequests
{
    public sealed class ExpertCollaborationRequest : Entity
    {
        public Guid Id { get; set; }
        public Guid ExpertProfileId { get; set; }
        public Guid StartupId { get; set; }
        public CollaborationRequestInitiator Initiator { get; set; }
        public CollaborationType CollaborationType { get; set; }
        public string? Message { get; set; }
        public int? ProposedHoursPerWeek { get; set; }
        public decimal? ProposedRate { get; set; }
        public ExpertCollaborationRequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// True when the expert side must respond — that is, when the startup opened the request.
        /// The mirror of this drives who may accept/reject, and its inverse who may withdraw.
        /// </summary>
        public bool AwaitsExpertResponse => Initiator == CollaborationRequestInitiator.Startup;

        public ExpertCollaborationRequest()
        {
        }

        public static ExpertCollaborationRequest Create(
            Guid expertProfileId,
            Guid startupId,
            CollaborationRequestInitiator initiator,
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
                Initiator = initiator,
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

        /// <summary>
        /// Times out a request nobody answered. Only the expiry job calls this; it is deliberately
        /// distinct from <see cref="Withdraw"/> so the history shows why the request ended.
        /// </summary>
        public Result Expire(DateTime utcNow)
        {
            if (Status != ExpertCollaborationRequestStatus.Pending)
            {
                return Result.Failure(ExpertCollaborationRequestErrors.MustBePending);
            }

            Status = ExpertCollaborationRequestStatus.Expired;
            UpdatedAt = utcNow;
            return Result.Success();
        }
    }
}
