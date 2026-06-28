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
    }
}
