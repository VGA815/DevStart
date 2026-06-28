using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Valuation.GetValuationBenchmarks
{
    /// <summary>
    /// The current benchmark set: for each (metric, sector, stage) key, the latest version whose
    /// <c>EffectiveFrom ≤ AsOf</c>. <see cref="AsOf"/> defaults to "now" when omitted, which lets an
    /// admin inspect what the engine would read today (or as of any past date).
    /// </summary>
    public sealed record GetValuationBenchmarksQuery(DateTime? AsOf = null)
        : IQuery<List<ValuationBenchmarkResponse>>;
}
