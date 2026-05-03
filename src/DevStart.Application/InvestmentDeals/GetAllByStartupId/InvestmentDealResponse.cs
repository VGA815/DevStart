using DevStart.Application.Abstractions.Validation;
using DevStart.Domain.InvestmentApplications;
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
        public InvestmentInstrument Instrument { get; init; }
        public decimal? ValuationCap { get; init; }
        public decimal? Discount { get; init; }
        public decimal? InterestRate { get; init; }
        public int? TermMonths { get; init; }
        public decimal? PreMoneyValuation { get; init; }
        public decimal LiquidationPreference { get; init; }
        public bool ProRataRights { get; init; }
        public IReadOnlyList<DealTermsFlag> Flags { get; set; } = Array.Empty<DealTermsFlag>();
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
    }
}
