using DevStart.Domain.InvestmentDeals;

namespace DevStart.Application.InvestmentDeals.GetAllByStartupId
{
    public sealed class InvestmentDealResponse
    {
        public Guid Id { get; init; }
        public Guid ApplicationId { get; init; }
        public Guid InvestorProfileId { get; init; }
        public string InvestorDisplayName { get; init; } = null!;
        public Guid StartupId { get; init; }
        public string StartupName { get; init; } = null!;
        public Guid? RoadmapItemId { get; init; }
        public string? RoadmapItemTitle { get; init; }
        public decimal Amount { get; init; }
        public bool ConfirmedByStartup { get; init; }
        public bool ConfirmedByInvestor { get; init; }
        public InvestmentDealStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
    }
}
