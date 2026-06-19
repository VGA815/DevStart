using DevStart.Application.Scoring.Tiers;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;

namespace DevStart.Application.Scoring
{
    internal sealed class ScoringEngine : IScoringEngine
    {
        public ScoreResult Compute(ScoringInputs inputs, DateTime calculatedAt)
        {
            decimal team = ComputeTeamScore(inputs.Members);
            decimal market = ComputeMarketScore(inputs.Tam, inputs.Sam, inputs.Som, inputs.MarketGrowthRate);
            decimal product = ComputeProductScore(inputs.Stage, inputs.HasPatents, inputs.Product, inputs.Roadmap);
            decimal traction = ComputeTractionScore(inputs.Traction);
            decimal competition = ComputeCompetitionScore(inputs.CompetitorsCount);

            ScoreWeights w = WeightsFor(inputs.Stage);

            decimal total = Round2(
                team * w.Team +
                market * w.Market +
                product * w.Product +
                traction * w.Traction +
                competition * w.Competition);

            return new ScoreResult(
                TotalScore: total,
                TeamScore: Round2(team),
                MarketScore: Round2(market),
                ProductScore: Round2(product),
                TractionScore: Round2(traction),
                CompetitionScore: Round2(competition),
                ValuationLow: 0m,
                ValuationHigh: 0m,
                MethodsUsed: Array.Empty<string>(),
                CalculatedAt: calculatedAt);
        }

        // Stage-aware weights: team/product matter most early, traction/market most later.
        // Each stage's weights sum to 1.00 (guarded by ScoringEngineTests). Proposed defaults — tunable.
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
        // NOTE: Tam is interpreted as USD here — the tier ranges ($1B / $10B) are dollar figures.
        // Valuation output (IValuationCalculator) is in RUB; the two are deliberately different currencies.
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

        // Spec: 0 competitors = BlueOcean (85), 1–3 = Niche (60), 4+ = High (35).
        private static decimal ComputeCompetitionScore(int competitorsCount)
        {
            if (competitorsCount == 0) return 85m;
            if (competitorsCount <= 3) return 60m;
            return 35m;
        }

        private static decimal Clamp(decimal value) =>
            value < 0m ? 0m : (value > 100m ? 100m : value);

        private static decimal Round2(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
