using DevStart.Domain.Startups;
using Microsoft.Extensions.Options;

namespace DevStart.Application.Scoring
{
    /// <summary>
    /// Stage-applicable ensemble of four valuation methods — Berkus, Scorecard, VC Method, Comparable —
    /// all RUB. Tunable constants come from <see cref="ValuationOptions"/>; the pre-money medians and
    /// revenue multiples come from the <see cref="ValuationBenchmarkSet"/> (DB-backed, versioned). The
    /// methodology version travels on every result for transparency/backtesting.
    ///
    /// Methodology:
    ///   Applicability   Berkus: Idea/PreSeed/Mvp · Scorecard: Idea–Seed (needs a median) ·
    ///                   VC: Mvp/Seed/SeriesA · Comparable: Mvp/Seed/SeriesA (needs a sector multiple
    ///                   and ARR &gt; 0).
    ///   Berkus          5 factors (idea, prototype, team, partnerships, traction), each a 0..1 signal ×
    ///                   a RUB ceiling; partnerships → 0 when absent.
    ///   Scorecard       stage/sector median × Σ(weightᵢ × multiplierᵢ), multiplierᵢ = clamp(0.5 + subᵢ/100,
    ///                   0.5..1.5). "Sales" factor is proxied by the traction sub-score. A sub-score with
    ///                   no data drops out and the factor weights are renormalized — feeding 0 in would
    ///                   silently apply the floor multiplier. No median on file → the method drops out of
    ///                   the ensemble (insufficient data, not a hardcoded floor).
    ///   VC Method       TV = exitRevenue × sector multiple; post-money = TV / (1+IRR)^n; pre-money =
    ///                   post-money − target round amount (when known). Exit revenue = ARR × growth, or a
    ///                   stage default when pre-revenue.
    ///   Comparable      sector revenue multiple × ARR; drops out when there is no multiple or no revenue.
    ///   Ensemble        weights renormalized to sum 1.0 over the methods that actually contribute.
    ///   Range           Low = min(point×(1−band), minMethod); High = max(point×(1+band), maxMethod);
    ///                   guarantees Low ≤ Point ≤ High.
    ///   Guardrails      negatives clamped to 0; when no method contributes → insufficient-data (empty/0).
    /// </summary>
    internal sealed class ValuationCalculator(IOptions<ValuationOptions> options) : IValuationCalculator
    {
        private readonly ValuationOptions _o = options.Value;

        public ValuationResult Compute(ScoreResult score, ScoringInputs inputs, ValuationBenchmarkSet benchmarks)
        {
            string version = _o.MethodologyVersion;
            StartupStage stage = inputs.Stage;

            var methods = new List<Method>();
            if (BerkusApplies(stage))
            {
                methods.Add(Berkus(score, inputs, _o.BerkusWeight));
            }
            if (ScorecardApplies(stage) && benchmarks.Median(inputs.Industry, stage) is { } median)
            {
                methods.Add(Scorecard(score, inputs, median, benchmarks.HasSectorMedian(inputs.Industry, stage), _o.ScorecardWeight));
            }
            if (VcApplies(stage))
            {
                methods.Add(Vc(inputs, _o.VcWeight));
            }
            if (ComparableApplies(stage))
            {
                decimal arr = Math.Max(0m, inputs.Traction.AnnualRecurringRevenue);
                if (arr > 0m && benchmarks.RevenueMultiple(inputs.Industry) is { } multiple)
                {
                    methods.Add(Comparable(inputs, multiple, arr, _o.ComparableWeight));
                }
            }

            if (methods.Count == 0)
            {
                return ValuationResult.InsufficientData(version);
            }

            decimal totalBaseWeight = methods.Sum(m => m.BaseWeight);
            if (totalBaseWeight <= 0m)
            {
                // Degenerate config (all weights 0) — fall back to equal weights so we never divide by 0.
                foreach (Method m in methods)
                {
                    m.BaseWeight = 1m;
                }
                totalBaseWeight = methods.Count;
            }

            var breakdown = new List<ValuationBreakdown>(methods.Count);
            decimal point = 0m;
            decimal minMethod = decimal.MaxValue;
            decimal maxMethod = decimal.MinValue;
            decimal weightAccumulator = 0m;

            for (int i = 0; i < methods.Count; i++)
            {
                Method m = methods[i];
                decimal exactWeight = m.BaseWeight / totalBaseWeight;
                point += m.Value * exactWeight;
                minMethod = Math.Min(minMethod, m.Value);
                maxMethod = Math.Max(maxMethod, m.Value);

                // Round each weight to 2dp for display, but hand the last method the residual so the
                // breakdown weights always sum to exactly 1.0 (0.33 + 0.33 + 0.34, never 0.99). The point
                // estimate above uses the unrounded weights, so it is unaffected.
                decimal weight = i == methods.Count - 1
                    ? 1.0m - weightAccumulator
                    : Round2(exactWeight);
                weightAccumulator += weight;
                breakdown.Add(new ValuationBreakdown(m.Name, m.Value, weight, m.Assumptions));
            }

            point = RoundRub(point);
            decimal band = Math.Max(0m, _o.RangeBand);
            decimal low = RoundRub(Math.Min(point * (1m - band), minMethod));
            decimal high = RoundRub(Math.Max(point * (1m + band), maxMethod));

            low = Math.Max(0m, low);
            high = Math.Max(low, high);

            string[] methodsUsed = methods.Select(m => m.Name).ToArray();
            return new ValuationResult(low, high, point, methodsUsed, breakdown, version);
        }

        // ---- Applicability matrix --------------------------------------------------------------

        private static bool BerkusApplies(StartupStage s) =>
            s is StartupStage.Idea or StartupStage.PreSeed or StartupStage.Mvp;

        private static bool ScorecardApplies(StartupStage s) =>
            s is StartupStage.Idea or StartupStage.PreSeed or StartupStage.Mvp or StartupStage.Seed;

        private static bool VcApplies(StartupStage s) =>
            s is StartupStage.Mvp or StartupStage.Seed or StartupStage.SeriesA;

        // Revenue-bearing stages. The ensemble adds Comparable only when a sector multiple exists and
        // ARR > 0, so the "Mvp only if revenue" rule falls out of the ARR gate at the call site.
        private static bool ComparableApplies(StartupStage s) =>
            s is StartupStage.Mvp or StartupStage.Seed or StartupStage.SeriesA;

        // ---- Berkus ----------------------------------------------------------------------------

        private Method Berkus(ScoreResult score, ScoringInputs inputs, decimal baseWeight)
        {
            BerkusOptions b = _o.Berkus;

            decimal idea = inputs.Product.HasArticulatedPositioning ? 1.0m : 0.5m;
            decimal prototype = Clamp01(PrototypeBaseline(inputs.Stage) + (inputs.HasPatents ? 0.2m : 0m));
            decimal team = Clamp01(score.TeamScore / 100m);
            decimal partnerships = inputs.HasStrategicPartnerships ? 1.0m : 0.0m;
            decimal traction = Clamp01(score.TractionScore / 100m);

            decimal value = RoundRub(
                idea * b.IdeaCeiling +
                prototype * b.PrototypeCeiling +
                team * b.TeamCeiling +
                partnerships * b.PartnershipsCeiling +
                traction * b.TractionCeiling);

            var assumptions = new List<string>
            {
                $"idea {idea:0.##}, prototype {prototype:0.##}, team {team:0.##}, traction {traction:0.##}"
            };
            assumptions.Add(inputs.HasStrategicPartnerships
                ? "strategic partnerships present"
                : "no strategic partnerships — factor zeroed");

            return new Method("Berkus", Math.Max(0m, value), baseWeight, assumptions);
        }

        private static decimal PrototypeBaseline(StartupStage stage) => stage switch
        {
            StartupStage.Idea => 0.2m,
            StartupStage.PreSeed => 0.5m,
            StartupStage.Mvp => 0.8m,
            StartupStage.Seed => 1.0m,
            StartupStage.SeriesA => 1.0m,
            _ => 0.2m
        };

        // ---- Scorecard -------------------------------------------------------------------------

        private Method Scorecard(ScoreResult score, ScoringInputs inputs, decimal median, bool sectorMedian, decimal baseWeight)
        {
            ScorecardOptions s = _o.Scorecard;
            const decimal neutral = 1.0m; // financing & "other": no direct signal

            // A sub-score that is null means "no data", not "worst case". Feeding 0 in would silently
            // hand that factor the floor multiplier (0.5) and drag the valuation down, so a no-data
            // factor is dropped and the Bill-Payne weights are renormalized over the rest — the same
            // rule the ensemble applies to its methods and the scoring engine to its factors.
            (string Name, decimal? Sub, decimal Weight)[] factors =
            [
                ("team", score.TeamScore, s.TeamWeight),
                ("market", score.MarketScore, s.MarketWeight),
                ("product", score.ProductScore, s.ProductWeight),
                ("competition", score.CompetitionScore, s.CompetitionWeight),
                ("sales", score.TractionScore, s.SalesWeight), // proxy: traction stands in for sales/marketing
            ];

            string[] dropped = factors.Where(f => f.Sub is null).Select(f => f.Name).ToArray();
            decimal droppedWeight = factors.Where(f => f.Sub is null).Sum(f => f.Weight);
            decimal keptWeight = factors.Where(f => f.Sub is not null).Sum(f => f.Weight)
                + s.FinancingWeight + s.OtherWeight;

            // Renormalize the surviving weights back to the original total so a dropped factor neither
            // shrinks nor inflates the composite.
            decimal renorm = keptWeight > 0m ? (keptWeight + droppedWeight) / keptWeight : 1m;

            decimal composite = renorm * (
                factors.Where(f => f.Sub is not null).Sum(f => f.Weight * Multiplier(f.Sub!.Value, s))
                + s.FinancingWeight * neutral
                + s.OtherWeight * neutral);

            decimal value = RoundRub(median * composite);

            var assumptions = new List<string>
            {
                $"median ₽{median:N0} ({(sectorMedian ? $"{inputs.Industry} {inputs.Stage}" : $"{inputs.Stage} (stage-only)")})",
                $"composite multiplier {composite:0.###}; sales proxied by traction"
            };
            if (dropped.Length > 0)
            {
                assumptions.Add($"no data for {string.Join(", ", dropped)} — factor(s) dropped, weights renormalized");
            }

            return new Method("Scorecard", Math.Max(0m, value), baseWeight, assumptions);
        }

        private static decimal Multiplier(decimal subScore, ScorecardOptions s)
        {
            // Tolerate a misconfigured floor/ceiling (floor > ceiling) instead of letting Math.Clamp throw.
            decimal low = Math.Min(s.MultiplierFloor, s.MultiplierCeiling);
            decimal high = Math.Max(s.MultiplierFloor, s.MultiplierCeiling);
            return Math.Clamp(0.5m + subScore / 100m, low, high);
        }

        // ---- VC Method -------------------------------------------------------------------------

        private Method Vc(ScoringInputs inputs, decimal baseWeight)
        {
            VcMethodOptions v = _o.Vc;
            decimal arr = Math.Max(0m, inputs.Traction.AnnualRecurringRevenue);

            bool preRevenue = arr <= 0m;
            decimal exitRevenue = preRevenue
                ? (v.PreRevenueExitRevenue.TryGetValue(inputs.Stage, out decimal d)
                    ? d
                    : v.DefaultPreRevenueExitRevenue)
                : arr * v.ExitRevenueGrowthMultiple;

            decimal multiple = v.SectorExitMultiples.TryGetValue(inputs.Industry, out decimal em)
                ? em
                : v.DefaultExitMultiple;
            decimal irr = v.StageIrr.TryGetValue(inputs.Stage, out decimal si) ? si : v.DefaultIrr;
            int n = Math.Max(1, v.HorizonYears);

            decimal terminalValue = exitRevenue * multiple;
            decimal discount = PowDecimal(1m + irr, n);
            decimal postMoney = discount > 0m ? terminalValue / discount : 0m;

            decimal value = postMoney;
            var assumptions = new List<string>
            {
                preRevenue
                    ? $"pre-revenue: assumed exit revenue ₽{exitRevenue:N0}"
                    : $"exit revenue ₽{exitRevenue:N0} (ARR ₽{arr:N0} × {v.ExitRevenueGrowthMultiple:0.##})",
                $"exit multiple {multiple:0.##}×, IRR {irr:P0}, horizon {n}y → post-money ₽{RoundRub(postMoney):N0}"
            };

            if (inputs.TargetRoundAmount is > 0m)
            {
                value = postMoney - inputs.TargetRoundAmount.Value;
                assumptions.Add($"pre-money = post-money − round ₽{inputs.TargetRoundAmount.Value:N0}");
            }

            return new Method("VcMethod", Math.Max(0m, RoundRub(value)), baseWeight, assumptions);
        }

        // ---- Comparable ------------------------------------------------------------------------

        // Market comparables: a sector EV/Revenue multiple applied to current ARR. The caller only
        // invokes this when both the multiple and ARR > 0 are present, so it never fabricates a value.
        private static Method Comparable(ScoringInputs inputs, decimal multiple, decimal arr, decimal baseWeight)
        {
            decimal value = RoundRub(multiple * arr);
            var assumptions = new List<string>
            {
                $"sector revenue multiple {multiple:0.##}× × ARR ₽{arr:N0}",
                $"metric: ARR (MRR × 12); sector {inputs.Industry}"
            };

            return new Method("Comparable", Math.Max(0m, value), baseWeight, assumptions);
        }

        // ---- Helpers ---------------------------------------------------------------------------

        private static decimal Clamp01(decimal v) => v < 0m ? 0m : (v > 1m ? 1m : v);

        private static decimal RoundRub(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);

        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        // Integer-exponent power in decimal — deterministic (no double rounding) for the VC discount factor.
        private static decimal PowDecimal(decimal @base, int exp)
        {
            decimal result = 1m;
            for (int i = 0; i < exp; i++)
            {
                result *= @base;
            }
            return result;
        }

        private sealed class Method(string name, decimal value, decimal baseWeight, IReadOnlyList<string> assumptions)
        {
            public string Name { get; } = name;
            public decimal Value { get; } = value;
            public decimal BaseWeight { get; set; } = baseWeight;
            public IReadOnlyList<string> Assumptions { get; } = assumptions;
        }
    }
}
