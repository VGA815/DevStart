using DevStart.Application.Scoring.Tiers;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;

namespace DevStart.Application.Scoring
{
    internal sealed class ScoringEngine : IScoringEngine
    {
        public ScoreResult Compute(ScoringInputs inputs, ValuationBenchmarkSet benchmarks, DateTime calculatedAt)
        {
            ScoreWeights w = WeightsFor(inputs.Stage);

            FactorOutcome team = ComputeTeamScore(inputs.Members);
            FactorOutcome market = ComputeMarketScore(inputs.Tam, inputs.Sam, inputs.Som, inputs.MarketGrowthRate);
            FactorOutcome product = ComputeProductScore(
                inputs.Stage, inputs.HasPatents, inputs.Product, inputs.Roadmap, inputs.HasRegistryCheckedIp,
                inputs.Traction);
            FactorOutcome traction = ComputeTractionScore(inputs.Traction);
            FactorOutcome competition = ComputeCompetitionScore(inputs.Competitors, inputs.Industry, benchmarks);

            ScoreFactor[] factors =
            [
                new("Team", team.Score, w.Team, team.Source, team.Detail),
                new("Market", market.Score, w.Market, market.Source, market.Detail),
                new("Product", product.Score, w.Product, product.Source, product.Detail),
                new("Traction", traction.Score, w.Traction, traction.Source, traction.Detail),
                new("Competition", competition.Score, w.Competition, competition.Source, competition.Detail,
                    Floor: CompetitionBaselineWithoutBenchmark),
            ];

            return Combine(factors, calculatedAt);
        }

        /// <summary>
        /// Combines the factors into a total: factors with no data (<c>Score is null</c>) drop out and
        /// the remaining weights are renormalized to sum 1.0 — the same rule the valuation ensemble
        /// applies to its methods.
        ///
        /// Renormalization makes "no data" equal to the weighted average of the other factors, which on
        /// the boundary "one competitor card → none" could sit *above* the real outcome and reward
        /// deleting the last card. So a dropped-out factor is also capped: the total can never exceed
        /// what it would have been with that factor scored at its floor. "No data" is therefore neutral
        /// but never better than the worst outcome the factor can actually produce.
        /// </summary>
        internal static ScoreResult Combine(IReadOnlyList<ScoreFactor> factors, DateTime calculatedAt)
        {
            ScoreFactor[] participating = factors.Where(f => f.Score.HasValue).ToArray();
            if (participating.Length == 0)
            {
                return ScoreResult.InsufficientData(calculatedAt);
            }

            // Degenerate weights (all zero) — fall back to equal shares so we never divide by 0.
            bool degenerate = participating.Sum(f => f.BaseWeight) <= 0m;
            decimal WeightOf(ScoreFactor f) => degenerate ? 1m : f.BaseWeight;

            decimal totalWeight = participating.Sum(WeightOf);
            decimal weightedSum = participating.Sum(f => f.Score!.Value * WeightOf(f));
            decimal total = weightedSum / totalWeight;

            // Ceiling rule for every factor that dropped out (see summary).
            foreach (ScoreFactor absent in factors.Where(f => !f.Score.HasValue && f.Floor.HasValue))
            {
                decimal absentWeight = WeightOf(absent);
                total = Math.Min(
                    total,
                    (weightedSum + absent.Floor!.Value * absentWeight) / (totalWeight + absentWeight));
            }

            // Round each displayed weight to 2dp but hand the last participant the residual, so the
            // breakdown weights always sum to exactly 1.0. The total above uses the unrounded weights.
            var breakdown = new List<ScoreFactorBreakdown>(factors.Count);
            decimal weightAccumulator = 0m;
            int participantsSeen = 0;
            foreach (ScoreFactor f in factors)
            {
                if (!f.Score.HasValue)
                {
                    breakdown.Add(new ScoreFactorBreakdown(f.Name, null, 0m, f.Source) { Detail = f.Detail });
                    continue;
                }

                participantsSeen++;
                decimal weight = participantsSeen == participating.Length
                    ? 1.0m - weightAccumulator
                    : Round2(WeightOf(f) / totalWeight);
                weightAccumulator += weight;
                breakdown.Add(
                    new ScoreFactorBreakdown(f.Name, Round2(f.Score.Value), weight, f.Source) { Detail = f.Detail });
            }

            decimal? Score(string name) => breakdown.Single(b => b.Factor == name).Score;

            return new ScoreResult(
                TotalScore: Round2(total),
                TeamScore: Score("Team") ?? 0m,
                MarketScore: Score("Market") ?? 0m,
                ProductScore: Score("Product") ?? 0m,
                TractionScore: Score("Traction") ?? 0m,
                CompetitionScore: Score("Competition"),
                ValuationLow: 0m,
                ValuationHigh: 0m,
                MethodsUsed: [],
                CalculatedAt: calculatedAt)
            {
                Factors = breakdown
            };
        }

        /// <summary>
        /// One factor on the way into <see cref="Combine"/>. <see cref="Score"/> is <c>null</c> for
        /// "no data"; <see cref="Floor"/> is the lowest score the factor can produce when it *does*
        /// have data, and backs the ceiling rule for the dropped-out case.
        /// </summary>
        internal readonly record struct ScoreFactor(
            string Name,
            decimal? Score,
            decimal BaseWeight,
            ScoreFactorSource Source,
            ScoreFactorDetail Detail,
            decimal? Floor = null);

        // Stage-aware weights: team/product matter most early, traction/market most later.
        // Each stage's weights sum to 1.00 (guarded by ScoringEngineTests); they are renormalized over
        // the factors that actually participate. Proposed defaults — tunable.
        internal readonly record struct ScoreWeights(
            decimal Team, decimal Market, decimal Product, decimal Traction, decimal Competition);

        internal static ScoreWeights WeightsFor(StartupStage stage) => stage switch
        {
            StartupStage.Idea or StartupStage.PreSeed => new(0.35m, 0.25m, 0.20m, 0.10m, 0.10m),
            StartupStage.Mvp or StartupStage.Seed => new(0.25m, 0.25m, 0.15m, 0.25m, 0.10m),
            StartupStage.SeriesA => new(0.20m, 0.25m, 0.10m, 0.35m, 0.10m),
            _ => new(0.35m, 0.25m, 0.20m, 0.10m, 0.10m)
        };

        // ---- Factor detail plumbing ---------------------------------------------------------------

        /// <summary>What a factor produced: its score (<c>null</c> = no data), provenance, and detail.</summary>
        private readonly record struct FactorOutcome(
            decimal? Score, ScoreFactorSource Source, ScoreFactorDetail Detail);

        /// <summary>
        /// Assembles a factor from its addends. The factor score *is* the sum of the components, so
        /// "components sum to the score" is not a rule that could drift — it is the only way the score
        /// is produced. The scale ceiling is applied as one more component with negative points, which
        /// keeps that identity true for the clamped factors too (see <see cref="ScoreComponent"/>).
        ///
        /// Codes are namespaced by factor here rather than at every call site, so the emitting code
        /// reads as the rule it implements (<c>Add("bonus.patents", 10m)</c>).
        /// </summary>
        private sealed class FactorBuilder(string factor)
        {
            private readonly List<ScoreComponent> _components = [];
            private readonly List<ScoreInput> _inputs = [];
            private readonly List<(string Code, decimal Points, IReadOnlyList<ScoreValue> Targets)> _hints = [];
            private decimal _points;

            /// <summary>Adds an addend: a base, a tier selection or a bonus.</summary>
            public FactorBuilder Add(string name, decimal points)
            {
                _components.Add(new ScoreComponent($"{factor}.{name}", points));
                _points += points;
                return this;
            }

            /// <summary>Records a raw value the formula read. Emitted even when the value is absent.</summary>
            public FactorBuilder In(string name, ScoreValue value)
            {
                _inputs.Add(new ScoreInput($"{factor}.input.{name}", value));
                return this;
            }

            /// <summary>
            /// Records an unmet condition and what it is worth on its own. Never call this for a
            /// condition the founder could satisfy by deleting data or overstating a self-declared
            /// figure — see docs/scoring-methodology.md and the policy test in the unit suite.
            /// </summary>
            public FactorBuilder Hint(string name, decimal points, params ScoreValue[] targets)
            {
                _hints.Add(($"{factor}.hint.{name}", points, targets));
                return this;
            }

            public FactorOutcome Build(ScoreFactorSource source)
            {
                if (_points > 100m)
                {
                    _components.Add(new ScoreComponent($"{factor}.clamp", 100m - _points));
                    _points = 100m;
                }
                else if (_points < 0m)
                {
                    _components.Add(new ScoreComponent($"{factor}.clamp", -_points));
                    _points = 0m;
                }

                // A hint may never promise points the scale cannot deliver, and one worth nothing is
                // not advice. Biggest win first; LINQ ordering is stable, so ties keep declaration order.
                decimal headroom = 100m - _points;
                ScoreHint[] hints = _hints
                    .Select(h => new ScoreHint(h.Code, Math.Min(h.Points, headroom), h.Targets))
                    .Where(h => h.Points > 0m)
                    .OrderByDescending(h => h.Points)
                    .ToArray();

                return new FactorOutcome(_points, source, new ScoreFactorDetail(_components, _inputs, hints));
            }

            /// <summary>
            /// The factor has no data and drops out of the weighting: no score, therefore no components
            /// and no headroom to cap against. The inputs still ship (so the reader sees *what* is
            /// missing), and any hint recorded becomes an <see cref="ScoreHint.EnablesFactor"/> one —
            /// its points are the score the factor would have, not a delta, because bringing the factor
            /// back changes the renormalization rather than just the sub-score.
            /// </summary>
            public FactorOutcome NoData()
            {
                ScoreHint[] hints = _hints
                    .Select(h => new ScoreHint(h.Code, h.Points, h.Targets, EnablesFactor: true))
                    .Where(h => h.Points > 0m)
                    .OrderByDescending(h => h.Points)
                    .ToArray();

                return new FactorOutcome(null, ScoreFactorSource.None, new ScoreFactorDetail([], _inputs, hints));
            }
        }

        // ---- Team ---------------------------------------------------------------------------------

        // Spec: highest founder tier base, +15 if CEO+CTO+CMO present.
        // No experience = 30, Industry = 60, Serial = 80, Serial with exit = 90.
        // Experience is taken from founders; if a team has no one flagged Founder, fall back to the
        // highest tier among all members so a founder-less team isn't unfairly capped at NoExperience.
        private static FactorOutcome ComputeTeamScore(IReadOnlyList<MemberInput> members)
        {
            var builder = new FactorBuilder("team");

            // Completeness bonus is role-agnostic: it rewards C-suite coverage regardless of Founder flag.
            bool hasCeo = members.Any(m => m.Position == StartupPosition.CEO);
            bool hasCto = members.Any(m => m.Position == StartupPosition.CTO);
            bool hasCmo = members.Any(m => m.Position == StartupPosition.CMO);

            builder
                .In("member_count", ScoreValue.Count(members.Count))
                .In("has_ceo", ScoreValue.Flag(hasCeo))
                .In("has_cto", ScoreValue.Flag(hasCto))
                .In("has_cmo", ScoreValue.Flag(hasCmo));

            if (members.Count == 0)
            {
                return builder
                    .In("founder_tier", ScoreValue.Absent)
                    .In("experience_pool", ScoreValue.Absent)
                    .Add("base.no_members", 0m)
                    .Hint("add_members", 30m)
                    .Build(ScoreFactorSource.None);
            }

            MemberInput[] founders = members.Where(m => m.Role == StartupRole.Founder).ToArray();
            bool foundersFlagged = founders.Length > 0;
            IReadOnlyList<MemberInput> experiencePool = foundersFlagged ? founders : members;

            FounderExperienceTier highest = FounderExperienceTier.NoExperience;
            foreach (MemberInput m in experiencePool)
            {
                FounderExperienceTier tier = ClassifyFounder(m);
                if ((int)tier > (int)highest)
                {
                    highest = tier;
                }
            }

            (string tierCode, decimal baseScore) = highest switch
            {
                FounderExperienceTier.IndustryExperience => ("industry_experience", 60m),
                FounderExperienceTier.Serial => ("serial", 80m),
                FounderExperienceTier.SerialWithExit => ("serial_with_exit", 90m),
                _ => ("no_experience", 30m)
            };

            builder
                .In("founder_tier", ScoreValue.Of($"founder_tier.{tierCode}"))
                .In("experience_pool", ScoreValue.Of(foundersFlagged ? "pool.founders" : "pool.all_members"))
                .Add($"base.{tierCode}", baseScore);

            if (hasCeo && hasCto && hasCmo)
            {
                builder.Add("bonus.csuite", 15m);
            }
            else
            {
                var missing = new List<ScoreValue>(3);
                if (!hasCeo) missing.Add(ScoreValue.Of("position.ceo"));
                if (!hasCto) missing.Add(ScoreValue.Of("position.cto"));
                if (!hasCmo) missing.Add(ScoreValue.Of("position.cmo"));
                builder.Hint("csuite", 15m, [.. missing]);
            }

            // Founder experience deliberately carries no hint: the platform never prompts for it, which
            // is exactly why it is the least gameable input we have (docs/scoring-inputs-audit.md).
            return builder.Build(ScoreFactorSource.SelfReported);
        }

        private static FounderExperienceTier ClassifyFounder(MemberInput m)
        {
            bool exit = m.HasPriorExit == true;
            int prior = m.PreviousStartupsCount ?? 0;
            int years = m.YearsOfExperience ?? 0;

            if (exit)
            {
                return FounderExperienceTier.SerialWithExit;
            }
            if (prior >= 1)
            {
                return FounderExperienceTier.Serial;
            }
            if (years >= 3)
            {
                return FounderExperienceTier.IndustryExperience;
            }
            return FounderExperienceTier.NoExperience;
        }

        // ---- Market -------------------------------------------------------------------------------

        // Spec: Sub1B=20, 1-10B=60, 10B+=90, plus CAGR bump (+10 / +25) and a funnel-consistency bump.
        // Tam/Sam/Som are entered and interpreted in RUB: the tier thresholds are ₽1B / ₽10B —
        // deliberate steps for the RUB-native platform, consistent with the RUB valuation output.
        private static FactorOutcome ComputeMarketScore(decimal? tam, decimal? sam, decimal? som, decimal? cagr)
        {
            var builder = new FactorBuilder("market");

            builder
                .In("tam", tam is > 0m ? ScoreValue.Money(tam.Value) : ScoreValue.Absent)
                .In("sam", sam is > 0m ? ScoreValue.Money(sam.Value) : ScoreValue.Absent)
                .In("som", som is > 0m ? ScoreValue.Money(som.Value) : ScoreValue.Absent)
                .In("cagr", cagr.HasValue ? ScoreValue.Percent(cagr.Value) : ScoreValue.Absent);

            if (tam is null || tam <= 0)
            {
                // The hint promises the *floor* tier, never the top one: filling the field in honestly
                // is worth advertising, typing a bigger number is not.
                return builder
                    .Add("base.no_tam", 0m)
                    .Hint("fill_tam", 20m)
                    .Build(ScoreFactorSource.None);
            }

            (string tamCode, decimal tamBase) = ClassifyTam(tam.Value) switch
            {
                MarketTamTier.From1To10B => ("tam_1_10b", 60m),
                MarketTamTier.Above10B => ("tam_10b_plus", 90m),
                _ => ("tam_sub_1b", 20m)
            };
            builder.Add($"base.{tamCode}", tamBase);

            if (cagr.HasValue)
            {
                switch (ClassifyCagr(cagr.Value))
                {
                    case MarketCagrTier.From10To20:
                        builder.Add("bonus.cagr_10_20", 10m);
                        break;
                    case MarketCagrTier.Above20:
                        builder.Add("bonus.cagr_20_plus", 25m);
                        break;
                }
            }

            if (FunnelBonus(tam.Value, sam, som) > 0m)
            {
                builder.Add("bonus.funnel", 5m);
            }
            else
            {
                builder.Hint("funnel", 5m);
            }

            // No hint for the TAM tier or for CAGR — both would read as "type a bigger number".
            return builder.Build(ScoreFactorSource.SelfReported);
        }

        // Proposed default — tunable. Rewards a credibly-scoped market: a defined obtainable slice that
        // forms a consistent funnel (0 < SOM <= SAM <= TAM). Missing/inconsistent SAM/SOM → no bonus.
        private static decimal FunnelBonus(decimal tam, decimal? sam, decimal? som)
        {
            if (sam is not > 0m || som is not > 0m)
            {
                return 0m;
            }
            bool consistent = som <= sam && sam <= tam;
            return consistent ? 5m : 0m;
        }

        private static MarketTamTier ClassifyTam(decimal tam)
        {
            if (tam >= 10_000_000_000m) return MarketTamTier.Above10B;
            if (tam >= 1_000_000_000m) return MarketTamTier.From1To10B;
            return MarketTamTier.Sub1B;
        }

        private static MarketCagrTier ClassifyCagr(decimal cagr)
        {
            if (cagr >= 20m) return MarketCagrTier.Above20;
            if (cagr >= 10m) return MarketCagrTier.From10To20;
            return MarketCagrTier.Below10;
        }

        // ---- Product ------------------------------------------------------------------------------

        // Spec: Idea=15, PreSeed=35, Mvp=60, Seed=75, SeriesA=85; +10 for patents.
        // Proposed defaults — tunable: +5 for articulated positioning (value proposition AND
        // differentiators filled in), +5 for evidence of planning (>= 3 roadmap items).
        private static FactorOutcome ComputeProductScore(
            StartupStage stage,
            bool hasPatents,
            ProductSignals product,
            RoadmapSignals roadmap,
            bool hasRegistryCheckedIp,
            TractionSignals traction)
        {
            var builder = new FactorBuilder("product");

            (string stageCode, decimal baseScore) = stage switch
            {
                StartupStage.PreSeed => ("pre_seed", 35m),
                StartupStage.Mvp => ("mvp", 60m),
                StartupStage.Seed => ("seed", 75m),
                StartupStage.SeriesA => ("series_a", 85m),
                _ => ("idea", 15m)
            };

            (string consistencyCode, bool stageBorneOut) = CrossCheckStage(stage, traction);

            builder
                .In("stage", ScoreValue.Of($"stage.{stageCode}"))
                .In("stage_consistency", ScoreValue.Of($"stage_consistency.{consistencyCode}"))
                .In("has_patents", ScoreValue.Flag(hasPatents))
                .In("has_positioning", ScoreValue.Flag(product.HasArticulatedPositioning))
                .In("roadmap_items", ScoreValue.Count(roadmap.ItemCount))
                .Add($"base.stage_{stageCode}", baseScore);

            // Patents still carry no hint. The register check (SC-62…66) made the *records* checkable,
            // but the checkbox beside them is still a one-click declaration worth +10, so prompting for
            // it would still be the platform inviting an unverifiable claim. The invitation to enter
            // numbers lives on the Product tab, where it costs nothing and promises nothing.
            if (hasPatents)
            {
                builder.Add("bonus.patents", 10m);
            }

            if (product.HasArticulatedPositioning)
            {
                builder.Add("bonus.positioning", 5m);
            }
            else
            {
                builder.Hint("positioning", 5m);
            }

            if (roadmap.ItemCount >= 3)
            {
                builder.Add("bonus.roadmap", 5m);
            }
            else
            {
                builder.Hint("roadmap", 5m, ScoreValue.Count(3));
            }

            // Stage itself carries no hint either — raising it moves the weights as well as this base,
            // so prompting a stage change would be prompting a misdeclaration.
            //
            // Both provenance flags ride on the source, never on the score: every component above is
            // untouched by them, so the factor's number is bit-for-bit the same with and without. They
            // exist so the investor can see which part of this factor rests on something other than
            // the founder's word — the register for the IP records, the platform's own metrics for the
            // declared stage.
            ScoreFactorSource source = ScoreFactorSource.SelfReported | ScoreFactorSource.PlatformDerived;
            if (hasRegistryCheckedIp)
            {
                source |= ScoreFactorSource.RegistryChecked;
            }
            if (stageBorneOut)
            {
                source |= ScoreFactorSource.CrossChecked;
            }

            return builder.Build(source);
        }

        /// <summary>
        /// Cross-checks the declared stage against the metrics on file. Idea/PreSeed/Mvp claim no
        /// traction, so there is nothing to contradict; Seed claims *some* (users or revenue) and
        /// SeriesA claims revenue. Returns the code that ships in the factor's inputs and whether the
        /// declaration was borne out.
        ///
        /// Nothing is blocked and no number moves — an overstated stage is a signal for the investor,
        /// not an input for the engine (М4 in docs/scoring-inputs-plan.md).
        ///
        /// "No metrics at all" lands in the same <c>unsupported</c> state as "metrics that fall short",
        /// deliberately. If silence read as "nothing to check", leaving the metrics tab empty would be
        /// the cheapest way to keep a declared stage unchallenged — the exact shape of gaming this
        /// mechanism exists to close. Which of the two it is stays visible regardless: the traction
        /// factor's own inputs already tell "absent" apart from "reported 0".
        ///
        /// A Revenue-proxied MRR counts as revenue here. Its period is undefined, which is why it never
        /// annualizes into the valuation's ARR anchor — but the question asked here is only whether
        /// money is coming in, and to that it is a perfectly good answer.
        /// </summary>
        private static (string Code, bool BorneOut) CrossCheckStage(StartupStage stage, TractionSignals traction) =>
            stage switch
            {
                StartupStage.Seed => traction.Mrr > 0m || traction.Mau > 0m
                    ? ("supported", true)
                    : ("unsupported", false),
                StartupStage.SeriesA => traction.Mrr > 0m
                    ? ("supported", true)
                    : ("unsupported", false),
                _ => ("not_applicable", false)
            };

        // ---- Traction -----------------------------------------------------------------------------

        // Spec:
        // - No revenue but MAU > 0: 35
        // - MRR > 0, declining (MoM < 0): 25
        // - MRR > 0, MoM 0–10%: 50
        // - MRR > 0, MoM 10–20%: 70
        // - MRR >= ₽1M with MoM >= 10%: 80
        // - MRR >= ₽4M with MoM >= 20%: 95
        // Signals are resolved upstream by IScoringDataProvider (incl. Revenue/Users/GrowthRate
        // fallbacks and the negative-MRR/MAU floor) — the engine just applies the tiers.
        //
        // Traction is the one tier-based factor: its score is a rung, not a sum of bonuses. It is
        // therefore emitted as a *single* component whose points are the score, rather than dressed up
        // as base + bonuses it does not have.
        private static FactorOutcome ComputeTractionScore(TractionSignals traction)
        {
            var builder = new FactorBuilder("traction");

            builder
                .In("mrr", traction.HasData ? ScoreValue.Money(traction.Mrr) : ScoreValue.Absent)
                .In("mrr_is_proxy", ScoreValue.Flag(traction.MrrIsProxy))
                .In("mau", traction.HasData ? ScoreValue.Count(traction.Mau) : ScoreValue.Absent)
                .In("mom_growth", traction.HasData ? ScoreValue.Percent(traction.MomGrowth) : ScoreValue.Absent);

            (string tierCode, decimal score) = ClassifyTraction(traction);
            builder.Add($"tier.{tierCode}", score);

            AddTractionHint(builder, tierCode, traction.MomGrowth, score);

            return builder.Build(traction.HasData ? ScoreFactorSource.SelfReported : ScoreFactorSource.None);
        }

        /// <summary>
        /// The traction ladder. The single source of both the score and the hint deltas, so the two can
        /// never desync. Rung order matches the original tier cascade exactly.
        /// </summary>
        private static (string Code, decimal Score) ClassifyTraction(TractionSignals t)
        {
            if (t.Mrr <= 0m)
            {
                return t.Mau > 0m ? ("users_only", 35m) : ("no_data", 0m);
            }
            if (t.Mrr >= 4_000_000m && t.MomGrowth >= 20m)
            {
                return ("scaling", 95m);
            }
            if (t.Mrr >= 1_000_000m && t.MomGrowth >= 10m)
            {
                return ("growing", 80m);
            }
            if (t.MomGrowth >= 10m)
            {
                return ("early_growth", 70m);
            }
            if (t.MomGrowth < 0m)
            {
                return ("declining", 25m);
            }
            return ("flat", 50m);
        }

        /// <summary>
        /// One hint per rung, pointing at the next rung only — the tab is an explanation, not a ladder
        /// diagram of every remaining step.
        /// </summary>
        private static void AddTractionHint(FactorBuilder builder, string tierCode, decimal mom, decimal score)
        {
            switch (tierCode)
            {
                case "no_data":
                    builder.Hint("first_users", 35m - score, ScoreValue.Count(1));
                    break;

                case "users_only":
                    // Starting to charge moves the startup onto the revenue rungs — which are selected by
                    // the same single MoM metric, whatever it was measuring pre-revenue. At MoM < 0 that
                    // lands on "declining" (25), *below* the current 35, so there is no honest gain to
                    // promise and the hint is suppressed. See docs/scoring-methodology.md.
                    if (mom >= 0m)
                    {
                        builder.Hint("first_revenue", (mom >= 10m ? 70m : 50m) - score);
                    }
                    break;

                case "declining":
                    builder.Hint("stop_decline", 50m - score, ScoreValue.Percent(0m));
                    break;

                case "flat":
                    builder.Hint("growth_10", 70m - score, ScoreValue.Percent(10m));
                    break;

                case "early_growth":
                    builder.Hint("mrr_1m", 80m - score, ScoreValue.Money(1_000_000m), ScoreValue.Percent(10m));
                    break;

                case "growing":
                    builder.Hint("mrr_4m", 95m - score, ScoreValue.Money(4_000_000m), ScoreValue.Percent(20m));
                    break;

                // "scaling" is the top rung — nothing left to advise.
            }
        }

        // ---- Competition --------------------------------------------------------------------------

        /// <summary>
        /// Baseline when no sector intensity benchmark is on file: the middle of the scale. We know
        /// nothing about how crowded the sector is, so the level is neutral and only the startup's own
        /// analysis moves it. Also the factor's floor — see the ceiling rule in <see cref="Combine"/>.
        /// </summary>
        private const decimal CompetitionBaselineWithoutBenchmark = 50m;

        /// <summary>Full bonus for a competitor analysis, reached at <see cref="CompetitorSaturationCount"/> cards.</summary>
        private const decimal MaxDocumentationBonus = 30m;

        /// <summary>Well-documented cards past which the analysis bonus stops growing.</summary>
        private const int CompetitorSaturationCount = 3;

        /// <summary>
        /// Bonus for the quality of the startup's competitor analysis: 10 / 20 / 30 points, saturating
        /// at three well-documented cards. Proposed defaults — tunable. The ladder itself is shared
        /// with the Berkus partnerships factor (<see cref="SaturatingCount"/>).
        /// </summary>
        private static decimal DocumentationBonus(int wellDocumentedCount) =>
            SaturatingCount.Of(MaxDocumentationBonus, wellDocumentedCount, CompetitorSaturationCount);

        /// <summary>
        /// Competition factor. Two independent components, neither of which is the number of cards:
        ///   (a) quality of the startup's own analysis — the count of *well-documented* cards
        ///       (saturating at 3), never the total count and never their share. Adding a card can only
        ///       help, deleting one can only hurt, and an unanalysed card is worth exactly nothing.
        ///   (b) how crowded the sector is — the external <c>CompetitionIntensity</c> benchmark
        ///       (0..100, 100 = maximally crowded), which the startup cannot edit.
        /// With no cards at all and no benchmark the factor has no data and drops out of the weighting
        /// (capped by the ceiling rule so an empty list is never better than an unanalysed one).
        /// See docs/scoring-methodology.md.
        /// </summary>
        private static FactorOutcome ComputeCompetitionScore(
            CompetitorSignals competitors, Industry industry, ValuationBenchmarkSet benchmarks)
        {
            var builder = new FactorBuilder("competition");
            decimal? intensity = benchmarks.CompetitionIntensity(industry);

            builder
                .In("total_cards", ScoreValue.Count(competitors.TotalCount))
                .In("documented_cards", ScoreValue.Count(competitors.WellDocumentedCount))
                .In("sector_intensity", intensity is { } known ? ScoreValue.Points(known) : ScoreValue.Absent);

            if (intensity is null && competitors.TotalCount == 0)
            {
                // Nothing to score, so the hint states the score the factor would have rather than a
                // delta — the weights renormalize when it comes back, and no delta is definable.
                return builder
                    .Hint(
                        "first_documented_card",
                        CompetitionBaselineWithoutBenchmark + DocumentationBonus(1),
                        ScoreValue.Count(1))
                    .NoData();
            }

            ScoreFactorSource source = ScoreFactorSource.None;

            if (intensity is { } value)
            {
                source |= ScoreFactorSource.ExternalBenchmark;
                builder.Add("base.benchmark", Clamp(100m - value));
            }
            else
            {
                builder.Add("base.neutral", CompetitionBaselineWithoutBenchmark);
            }

            // Always emitted, including at +0: unlike the boolean bonuses elsewhere this is a graded
            // axis, and showing the rung the startup is on is what makes the hint below legible.
            decimal bonus = DocumentationBonus(competitors.WellDocumentedCount);
            builder.Add("bonus.documented", bonus);

            if (competitors.TotalCount > 0)
            {
                source |= ScoreFactorSource.SelfReported;
            }

            if (competitors.WellDocumentedCount < CompetitorSaturationCount)
            {
                int next = competitors.WellDocumentedCount + 1;
                builder.Hint("document_card", DocumentationBonus(next) - bonus, ScoreValue.Count(next));
            }

            // The total number of cards gets no hint — that is precisely the driver v5 removed.
            return builder.Build(source);
        }

        // ---- Helpers ----------------------------------------------------------------------------

        private static decimal Clamp(decimal value) =>
            value < 0m ? 0m : (value > 100m ? 100m : value);

        private static decimal Round2(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
