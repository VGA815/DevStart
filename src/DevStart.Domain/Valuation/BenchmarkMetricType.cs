namespace DevStart.Domain.Valuation
{
    /// <summary>
    /// Kind of valuation benchmark stored in <see cref="ValuationBenchmark"/>.
    /// </summary>
    public enum BenchmarkMetricType
    {
        /// <summary>Median pre-money valuation (RUB) for a sector/stage pair.</summary>
        PreMoneyMedian = 0,

        /// <summary>EV/Revenue multiple for a sector (stage-agnostic).</summary>
        RevenueMultiple = 1,

        /// <summary>
        /// Competition intensity of a sector (stage-agnostic), 0..100 where 100 means a maximally
        /// crowded sector. Read by the scoring engine's competition factor: it is the external half of
        /// that factor, so the score no longer rests solely on what the startup declares about itself.
        /// </summary>
        CompetitionIntensity = 2,
    }
}
