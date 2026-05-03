using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentApplications;

namespace DevStart.Application.InvestmentApplications.Create
{
    public sealed class CreateInvestmentApplicationCommand : ICommand<Guid>
    {
        public Guid StartupId { get; set; }
        public Guid? RoadmapItemId { get; set; }
        public decimal Amount { get; set; }
        public string? Message { get; set; }
        public InvestmentInstrument Instrument { get; set; } = InvestmentInstrument.Safe;
        public decimal? ValuationCap { get; set; }
        public decimal? Discount { get; set; }
        public decimal? InterestRate { get; set; }
        public int? TermMonths { get; set; }
        public decimal? PreMoneyValuation { get; set; }
        public decimal LiquidationPreference { get; set; } = 1.0m;
        public bool ProRataRights { get; set; }

        public CreateInvestmentApplicationCommand(
            Guid startupId,
            Guid? roadmapItemId,
            decimal amount,
            string? message,
            InvestmentInstrument instrument = InvestmentInstrument.Safe,
            decimal? valuationCap = null,
            decimal? discount = null,
            decimal? interestRate = null,
            int? termMonths = null,
            decimal? preMoneyValuation = null,
            decimal liquidationPreference = 1.0m,
            bool proRataRights = false)
        {
            StartupId = startupId;
            RoadmapItemId = roadmapItemId;
            Amount = amount;
            Message = message;
            Instrument = instrument;
            ValuationCap = valuationCap;
            Discount = discount;
            InterestRate = interestRate;
            TermMonths = termMonths;
            PreMoneyValuation = preMoneyValuation;
            LiquidationPreference = liquidationPreference;
            ProRataRights = proRataRights;
        }
    }
}
