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

            decimal team = ComputeTeamScore(inputs.Members);
            decimal market = ComputeMarketScore(inputs.Tam, inputs.Sam, inputs.Som, inputs.MarketGrowthRate);
            decimal product = ComputeProductScore(inputs.Stage, inputs.HasPatents, inputs.Product, inputs.Roadmap);
            decimal traction = ComputeTractionScore(inputs.Traction);
            CompetitionFactor competition = ComputeCompetitionScore(inputs.Competitors, inputs.Industry, benchmarks);

            ScoreFactor[] factors =
            [
                new("Team", team, w.Team,
                    inputs.Members.Count > 0 ? ScoreFactorSource.SelfReported : ScoreFactorSource.None,
                    [inputs.Members.Count > 0
                        ? $"{inputs.Members.Count} member(s) on file"
                        : "no members on file — scored 0"]),

                new("Market", market, w.Market,
                    inputs.Tam is > 0m ? ScoreFactorSource.SelfReported : ScoreFactorSource.None,
                    [inputs.Tam is > 0m
                        ? $"TAM ₽{inputs.Tam:N0}"
                        : "no TAM on file — scored 0"]),

                // Stage is mandatory on a startup, so this factor always has a basis; the roadmap part
                // is platform data (item count), the rest is declared by the startup.
                new("Product", product, w.Product,
                    ScoreFactorSource.SelfReported | ScoreFactorSource.PlatformDerived,
                    [$"stage {inputs.Stage}, {inputs.Roadmap.ItemCount} roadmap item(s)"]),

                new("Traction", traction, w.Traction,
                    inputs.Traction.HasData ? ScoreFactorSource.SelfReported : ScoreFactorSource.None,
                    [inputs.Traction.HasData
                        ? $"MRR ₽{inputs.Traction.Mrr:N0}, MAU {inputs.Traction.Mau:N0}, MoM {inputs.Traction.MomGrowth:0.##}%"
                        : "no metrics on file — scored 0"]),

                new("Competition", competition.Score, w.Competition, competition.Source, competition.Notes,
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
                    breakdown.Add(new ScoreFactorBreakdown(f.Name, null, 0m, f.Source, f.Notes));
                    continue;
                }

                participantsSeen++;
                decimal weight = participantsSeen == participating.Length
                    ? 1.0m - weightAccumulator
                    : Round2(WeightOf(f) / totalWeight);
                weightAccumulator += weight;
                breakdown.Add(new ScoreFactorBreakdown(f.Name, Round2(f.Score.Value), weight, f.Source, f.Notes));
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
            IReadOnlyList<string> Notes,
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

        // Spec: highest founder tier base, +15 if CEO+CTO+CMO present.
        // No experience = 30, Industry = 60, Serial = 80, Serial with exit = 90.
        // Experience is taken from founders; if a team has no one flagged Founder, fall back to the
        // highest tier among all members so a founder-less team isn't unfairly capped at NoExperience.
        private static decimal ComputeTeamScore(IReadOnlyList<MemberInput> members)
        {
            if (members.Count == 0)
            {
                return 0m;
            }

            IEnumerable<MemberInput> experiencePool = members.Where(m => m.Role == StartupRole.Founder);
            if (!experiencePool.Any())
            {
                experiencePool = members;
            }

            FounderExperienceTier highest = FounderExperienceTier.NoExperience;
            foreach (MemberInput m in experiencePool)
            {
                FounderExperienceTier tier = ClassifyFounder(m);
                if ((int)tier > (int)highest)
                {
                    highest = tier;
                }
            }

            decimal baseScore = highest switch
            {
                FounderExperienceTier.NoExperience => 30m,
                FounderExperienceTier.IndustryExperience => 60m,
                FounderExperienceTier.Serial => 80m,
                FounderExperienceTier.SerialWithExit => 90m,
                _ => 30m
            };

            // Completeness bonus is role-agnostic: it rewards C-suite coverage regardless of Founder flag.
            bool hasCeo = members.Any(m => m.Position == StartupPosition.CEO);
            bool hasCto = members.Any(m => m.Position == StartupPosition.CTO);
            bool hasCmo = members.Any(m => m.Position == StartupPosition.CMO);
            decimal completenessBonus = (hasCeo && hasCto && hasCmo) ? 15m : 0m;

            return Clamp(baseScore + completenessBonus);
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

        // Spec: Sub1B=20, 1-10B=60, 10B+=90, plus CAGR bump (+10 / +25) and a funnel-consistency bump.
        // Tam/Sam/Som are entered and interpreted in RUB: the tier thresholds are ₽1B / ₽10B —
        // deliberate steps for the RUB-native platform, consistent with the RUB valuation output.
        private static decimal ComputeMarketScore(decimal? tam, decimal? sam, decimal? som, decimal? cagr)
        {
            if (tam is null || tam <= 0)
            {
                return 0m;
            }

            MarketTamTier tamTier = ClassifyTam(tam.Value);
            decimal tamBase = tamTier switch
            {
                MarketTamTier.Sub1B => 20m,
                MarketTamTier.From1To10B => 60m,
                MarketTamTier.Above10B => 90m,
                _ => 0m
            };

            decimal cagrBump = 0m;
            if (cagr.HasValue)
            {
                MarketCagrTier cagrTier = ClassifyCagr(cagr.Value);
                cagrBump = cagrTier switch
                {
                    MarketCagrTier.Below10 => 0m,
                    MarketCagrTier.From10To20 => 10m,
                    MarketCagrTier.Above20 => 25m,
                    _ => 0m
                };
            }

            return Clamp(tamBase + cagrBump + FunnelBonus(tam.Value, sam, som));
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

        // Spec: Idea=15, PreSeed=35, Mvp=60, Seed=75, SeriesA=85; +10 for patents.
        // Proposed defaults — tunable: +5 for articulated positioning (value proposition AND
        // differentiators filled in), +5 for evidence of planning (>= 3 roadmap items).
        private static decimal ComputeProductScore(StartupStage stage, bool hasPatents, ProductSignals product, RoadmapSignals roadmap)
        {
            decimal baseScore = stage switch
            {
                StartupStage.Idea => 15m,
                StartupStage.PreSeed => 35m,
                StartupStage.Mvp => 60m,
                StartupStage.Seed => 75m,
                StartupStage.SeriesA => 85m,
                _ => 15m
            };
            decimal patentBonus = hasPatents ? 10m : 0m;
            decimal articulationBonus = product.HasArticulatedPositioning ? 5m : 0m;
            decimal planningBonus = roadmap.ItemCount >= 3 ? 5m : 0m;
            return Clamp(baseScore + patentBonus + articulationBonus + planningBonus);
        }

        // Spec:
        // - No revenue but MAU > 0: 35
        // - MRR > 0, declining (MoM < 0): 25
        // - MRR > 0, MoM 0–10%: 50
        // - MRR > 0, MoM 10–20%: 70
        // - MRR >= ₽1M with MoM >= 10%: 80
        // - MRR >= ₽4M with MoM >= 20%: 95
        // Signals are resolved upstream by IScoringDataProvider (incl. Revenue/Users/GrowthRate
        // fallbacks and the negative-MRR/MAU floor) — the engine just applies the tiers.
        private static decimal ComputeTractionScore(TractionSignals traction)
        {
            decimal mrr = traction.Mrr;
            decimal mau = traction.Mau;
            decimal mom = traction.MomGrowth;

            if (mrr <= 0)
            {
                return mau > 0 ? 35m : 0m;
            }

            if (mrr >= 4_000_000m && mom >= 20m)
            {
                return 95m;
            }
            if (mrr >= 1_000_000m && mom >= 10m)
            {
                return 80m;
            }
            if (mom >= 10m)
            {
                return 70m;
            }
            if (mom < 0m)
            {
                return 25m;
            }
            return 50m;
        }

        // ---- Competition ------------------------------------------------------------------------

        /// <summary>
        /// Baseline when no sector intensity benchmark is on file: the middle of the scale. We know
        /// nothing about how crowded the sector is, so the level is neutral and only the startup's own
        /// analysis moves it. Also the factor's floor — see the ceiling rule in <see cref="Combine"/>.
        /// </summary>
        private const decimal CompetitionBaselineWithoutBenchmark = 50m;

        /// <summary>Bonus per well-documented competitor card, saturating at three. Proposed defaults — tunable.</summary>
        private static decimal DocumentationBonus(int wellDocumentedCount) => wellDocumentedCount switch
        {
            <= 0 => 0m,
            1 => 10m,
            2 => 20m,
            _ => 30m
        };

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
        private static CompetitionFactor ComputeCompetitionScore(
            CompetitorSignals competitors, Industry industry, ValuationBenchmarkSet benchmarks)
        {
            decimal? intensity = benchmarks.CompetitionIntensity(industry);

            if (intensity is null && competitors.TotalCount == 0)
            {
                return new CompetitionFactor(
                    null,
                    ScoreFactorSource.None,
                    ["no competitor cards and no sector intensity benchmark — factor excluded, weights renormalized"]);
            }

            decimal baseScore = intensity is { } value
                ? Clamp(100m - value)
                : CompetitionBaselineWithoutBenchmark;

            decimal bonus = DocumentationBonus(competitors.WellDocumentedCount);

            ScoreFactorSource source = ScoreFactorSource.None;
            var notes = new List<string>();

            if (intensity is { } i)
            {
                source |= ScoreFactorSource.ExternalBenchmark;
                notes.Add($"sector intensity {i:0.##}/100 → base {baseScore:0.##}");
            }
            else
            {
                notes.Add($"no sector intensity benchmark → neutral base {baseScore:0.##}");
            }

            if (competitors.TotalCount > 0)
            {
                source |= ScoreFactorSource.SelfReported;
            }
            notes.Add(
                $"{competitors.WellDocumentedCount} of {competitors.TotalCount} competitor card(s) documented → +{bonus:0.##}"
                + " (count of cards is not a scoring driver)");

            return new CompetitionFactor(Clamp(baseScore + bonus), source, notes);
        }

        private readonly record struct CompetitionFactor(
            decimal? Score, ScoreFactorSource Source, IReadOnlyList<string> Notes);

        // ---- Helpers ----------------------------------------------------------------------------

        private static decimal Clamp(decimal value) =>
            value < 0m ? 0m : (value > 100m ? 100m : value);

        private static decimal Round2(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
