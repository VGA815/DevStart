using DevStart.Domain.Startups;
using Microsoft.Extensions.Options;

namespace DevStart.Application.Scoring
{
    /// <summary>
    /// Stage-applicable ensemble of three valuation methods — Berkus, Scorecard, VC Method — all RUB.
    /// All constants come from <see cref="ValuationOptions"/> (tunable); the methodology version travels
    /// on every result for transparency/backtesting.
    ///
    /// Methodology (SC-01):
    ///   Applicability   Berkus: Idea/PreSeed/Mvp · Scorecard: Idea–Seed · VC: Mvp/Seed/SeriesA.
    ///   Berkus          5 factors (idea, prototype, team, partnerships, traction), each a 0..1 signal ×
    ///                   a RUB ceiling; partnerships → 0 when absent.
    ///   Scorecard       stage/sector median × Σ(weightᵢ × multiplierᵢ), multiplierᵢ = clamp(0.5 + subᵢ/100,
    ///                   0.5..1.5). "Sales" factor is proxied by the traction sub-score.
    ///   VC Method       TV = exitRevenue × sector multiple; post-money = TV / (1+IRR)^n; pre-money =
    ///                   post-money − target round amount (when known). Exit revenue = ARR × growth, or a
    ///                   stage default when pre-revenue.
    ///   Ensemble        weights renormalized to sum 1.0 over the applicable methods.
    ///   Range           Low = min(point×(1−band), minMethod); High = max(point×(1+band), maxMethod);
    ///                   guarantees Low ≤ Point ≤ High.
    ///   Guardrails      negatives clamped to 0; when no method applies → insufficient-data (empty/0).
    /// </summary>
    internal sealed class ValuationCalculator(IOptions<ValuationOptions> options) : IValuationCalculator
    {
        private readonly ValuationOptions _o = options.Value;

        public ValuationResult Compute(ScoreResult score, ScoringInputs inputs)
        {
            string version = _o.MethodologyVersion;
            StartupStage stage = inputs.Stage;

            var methods = new List<Method>();
            if (BerkusApplies(stage))
            {
                methods.Add(Berkus(score, inputs, _o.BerkusWeight));
            }
            if (ScorecardApplies(stage))
            {
                methods.Add(Scorecard(score, inputs, _o.ScorecardWeight));
            }
            if (VcApplies(stage))
            {
                methods.Add(Vc(inputs, _o.VcWeight));
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

        private Method Scorecard(ScoreResult score, ScoringInputs inputs, decimal baseWeight)
        {
            ScorecardOptions s = _o.Scorecard;
            decimal median = Median(inputs.Stage, inputs.Industry);

            decimal mTeam = Multiplier(score.TeamScore, s);
            decimal mMarket = Multiplier(score.MarketScore, s);
            decimal mProduct = Multiplier(score.ProductScore, s);
            decimal mCompetition = Multiplier(score.CompetitionScore, s);
            decimal mSales = Multiplier(score.TractionScore, s); // proxy: traction stands in for sales/marketing
            const decimal neutral = 1.0m;                          // financing & "other": no direct signal

            decimal composite =
                s.TeamWeight * mTeam +
                s.MarketWeight * mMarket +
                s.ProductWeight * mProduct +
                s.CompetitionWeight * mCompetition +
                s.SalesWeight * mSales +
                s.FinancingWeight * neutral +
                s.OtherWeight * neutral;

            decimal value = RoundRub(median * composite);

            bool sectorMedian = inputs.Industry != Industry.Other
                && s.SectorStageMedians.TryGetValue(inputs.Industry, out Dictionary<StartupStage, decimal>? byStage)
                && byStage.ContainsKey(inputs.Stage);
            var assumptions = new List<string>
            {
                $"median ₽{median:N0} ({(sectorMedian ? $"{inputs.Industry} {inputs.Stage}" : $"{inputs.Stage} (stage-only)")})",
                $"composite multiplier {composite:0.###}; sales proxied by traction"
            };

            return new Method("Scorecard", Math.Max(0m, value), baseWeight, assumptions);
        }

        private decimal Median(StartupStage stage, Industry industry)
        {
            ScorecardOptions s = _o.Scorecard;
            if (industry != Industry.Other &&
                s.SectorStageMedians.TryGetValue(industry, out Dictionary<StartupStage, decimal>? byStage) &&
                byStage.TryGetValue(stage, out decimal sectorMedian))
            {
                return sectorMedian;
            }
            return s.StageMedians.TryGetValue(stage, out decimal stageMedian)
                ? stageMedian
                : 60_000_000m;
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
