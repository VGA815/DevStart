using DevStart.SharedKernel;

namespace DevStart.Domain.InvestmentApplications
{
    public sealed class InvestmentApplication : Entity
    {
        public Guid Id { get; set; }
        public Guid InvestorProfileId { get; set; }
        public Guid StartupId { get; set; }
        public Guid? RoadmapItemId { get; set; }
        public decimal Amount { get; set; }
        public string? Message { get; set; }
        public InvestmentApplicationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public InvestmentApplication()
        {
        }

        public static InvestmentApplication Create(
            Guid investorProfileId,
            Guid startupId,
            Guid? roadmapItemId,
            decimal amount,
            string? message,
            DateTime createdAt)
            => new()
            {
                Id = Guid.NewGuid(),
                InvestorProfileId = investorProfileId,
                StartupId = startupId,
                RoadmapItemId = roadmapItemId,
                Amount = amount,
                Message = message,
                Status = InvestmentApplicationStatus.Pending,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

        public Result Accept(DateTime utcNow)
        {
            if (Status != InvestmentApplicationStatus.Pending)
            {
                return Result.Failure(InvestmentApplicationErrors.MustBePending);
            }

            Status = InvestmentApplicationStatus.Accepted;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        public Result Reject(DateTime utcNow)
        {
            if (Status != InvestmentApplicationStatus.Pending)
            {
                return Result.Failure(InvestmentApplicationErrors.MustBePending);
            }

            Status = InvestmentApplicationStatus.Rejected;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        public Result Withdraw(DateTime utcNow)
        {
            if (Status != InvestmentApplicationStatus.Pending)
            {
                return Result.Failure(InvestmentApplicationErrors.MustBePending);
            }

            Status = InvestmentApplicationStatus.Withdrawn;
            UpdatedAt = utcNow;
            return Result.Success();
        }
    }
}
