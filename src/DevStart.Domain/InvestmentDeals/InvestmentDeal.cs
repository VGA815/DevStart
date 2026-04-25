using DevStart.Domain.InvestmentApplications;
using DevStart.SharedKernel;

namespace DevStart.Domain.InvestmentDeals
{
    public sealed class InvestmentDeal : Entity
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid InvestorProfileId { get; set; }
        public Guid StartupId { get; set; }
        public Guid? RoadmapItemId { get; set; }
        public decimal Amount { get; set; }
        public bool ConfirmedByStartup { get; set; }
        public bool ConfirmedByInvestor { get; set; }
        public InvestmentDealStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public InvestmentDeal()
        {
        }

        public static InvestmentDeal CreateFromApplication(InvestmentApplication application, DateTime utcNow)
            => new()
            {
                Id = Guid.NewGuid(),
                ApplicationId = application.Id,
                InvestorProfileId = application.InvestorProfileId,
                StartupId = application.StartupId,
                RoadmapItemId = application.RoadmapItemId,
                Amount = application.Amount,
                ConfirmedByStartup = false,
                ConfirmedByInvestor = false,
                Status = InvestmentDealStatus.InProgress,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
                CompletedAt = null
            };

        public Result ConfirmByStartup(DateTime utcNow)
        {
            if (Status == InvestmentDealStatus.Completed)
            {
                return Result.Failure(InvestmentDealErrors.AlreadyCompleted);
            }

            if (Status == InvestmentDealStatus.Cancelled)
            {
                return Result.Failure(InvestmentDealErrors.AlreadyCancelled);
            }

            if (ConfirmedByStartup)
            {
                return Result.Failure(InvestmentDealErrors.AlreadyConfirmed);
            }

            ConfirmedByStartup = true;
            UpdatedAt = utcNow;

            TryComplete(utcNow);
            return Result.Success();
        }

        public Result ConfirmByInvestor(DateTime utcNow)
        {
            if (Status == InvestmentDealStatus.Completed)
            {
                return Result.Failure(InvestmentDealErrors.AlreadyCompleted);
            }

            if (Status == InvestmentDealStatus.Cancelled)
            {
                return Result.Failure(InvestmentDealErrors.AlreadyCancelled);
            }

            if (ConfirmedByInvestor)
            {
                return Result.Failure(InvestmentDealErrors.AlreadyConfirmed);
            }

            ConfirmedByInvestor = true;
            UpdatedAt = utcNow;

            TryComplete(utcNow);
            return Result.Success();
        }

        private void TryComplete(DateTime utcNow)
        {
            if (ConfirmedByStartup && ConfirmedByInvestor && Status == InvestmentDealStatus.InProgress)
            {
                Status = InvestmentDealStatus.Completed;
                CompletedAt = utcNow;
                Raise(new InvestmentDealCompletedDomainEvent(Id, ApplicationId, InvestorProfileId, StartupId));
            }
        }
    }
}
