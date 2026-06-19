using DevStart.Application.Scoring;
using Shouldly;

namespace DevStart.UnitTests.Application.Scoring;

public sealed class TractionSignalsTests
{
    [Fact]
    public void From_FloorsNegativeMrrAndMau_ButKeepsMomSigned()
    {
        TractionSignals signals = TractionSignals.From(mrr: -100m, mau: -10m, momGrowth: -5m);

        signals.Mrr.ShouldBe(0m);
        signals.Mau.ShouldBe(0m);
        signals.MomGrowth.ShouldBe(-5m); // a decline is a legitimate signal, not dirty input
    }

    [Fact]
    public void From_TreatsNullsAsZero()
    {
        TractionSignals signals = TractionSignals.From(mrr: null, mau: null, momGrowth: null);

        signals.ShouldBe(TractionSignals.Empty);
    }

    [Fact]
    public void AnnualRecurringRevenue_IsMonthlyTimesTwelve_AndNeverNegative()
    {
        TractionSignals.From(mrr: 1_000_000m, mau: 0m, momGrowth: 0m)
            .AnnualRecurringRevenue.ShouldBe(12_000_000m);

        // Negative MRR is floored before annualizing, so ARR can never go below 0.
        TractionSignals.From(mrr: -100m, mau: 0m, momGrowth: 0m)
            .AnnualRecurringRevenue.ShouldBe(0m);
    }
}
