using DevStart.SharedKernel;

namespace DevStart.Domain.Startups
{
    /// <summary>
    /// Persisted point-in-time score + valuation for a startup. Written whenever a valuation is
    /// (re)computed for a durable purpose (term-sheet generation, on-demand recompute), giving the
    /// platform a history for backtesting and a provenance anchor (<see cref="CalculatedAt"/>,
    /// <see cref="MethodologyVersion"/>) for documents. Live reads still recompute + cache; this table
    /// is the audit/history trail, not the read path.
    /// </summary>
    public sealed class StartupValuationSnapshot : Entity
    {
        public Guid Id { get; set; }
        public Guid StartupId { get; set; }

        public decimal TotalScore { get; set; }
        public decimal TeamScore { get; set; }
        public decimal MarketScore { get; set; }
        public decimal ProductScore { get; set; }
        public decimal TractionScore { get; set; }

        /// <summary>
        /// <c>null</c> when the competition factor had no data and was excluded from the weighting —
        /// a snapshot must not record "no data" as a 0 that reads like the worst possible outcome.
        /// </summary>
        public decimal? CompetitionScore { get; set; }

        public decimal ValuationLow { get; set; }
        public decimal ValuationHigh { get; set; }
        public decimal ValuationPoint { get; set; }

        /// <summary>Comma-separated names of the methods that contributed to the ensemble.</summary>
        public string MethodsUsed { get; set; } = null!;

        /// <summary>JSON-serialized per-method breakdown (name/value/weight/assumptions). Optional.</summary>
        public string? BreakdownJson { get; set; }

        public string MethodologyVersion { get; set; } = null!;
        public DateTime CalculatedAt { get; set; }

        public static StartupValuationSnapshot Create(
            Guid startupId,
            decimal totalScore,
            decimal teamScore,
            decimal marketScore,
            decimal productScore,
            decimal tractionScore,
            decimal? competitionScore,
            decimal valuationLow,
            decimal valuationHigh,
            decimal valuationPoint,
            string methodsUsed,
            string? breakdownJson,
            string methodologyVersion,
            DateTime calculatedAt)
            => new()
            {
                Id = Guid.NewGuid(),
                StartupId = startupId,
                TotalScore = totalScore,
                TeamScore = teamScore,
                MarketScore = marketScore,
                ProductScore = productScore,
                TractionScore = tractionScore,
                CompetitionScore = competitionScore,
                ValuationLow = valuationLow,
                ValuationHigh = valuationHigh,
                ValuationPoint = valuationPoint,
                MethodsUsed = methodsUsed,
                BreakdownJson = breakdownJson,
                MethodologyVersion = methodologyVersion,
                CalculatedAt = calculatedAt
            };

        public StartupValuationSnapshot() { }
    }
}
