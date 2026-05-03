using DevStart.Application.Scoring.Tiers;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.Startups;

namespace DevStart.Application.Scoring
{
    internal sealed class ScoringEngine : IScoringEngine
    {
        // Weights from the spec:
        // Score = Team × 0.30 + Market × 0.25 + Product × 0.15 + Traction × 0.15 + Competition × 0.10
        private const decimal TeamWeight = 0.30m;
        private const decimal MarketWeight = 0.25m;
        private const decimal ProductWeight = 0.15m;
        private const decimal TractionWeight = 0.15m;
        private const decimal CompetitionWeight = 0.10m;

        public ScoreResult Compute(ScoringInputs inputs, DateTime calculatedAt)
        {
            decimal team = ComputeTeamScore(inputs.Members);
            decimal market = ComputeMarketScore(inputs.Tam, inputs.MarketGrowthRate);
            decimal product = ComputeProductScore(inputs.Stage, inputs.HasPatents);
            decimal traction = ComputeTractionScore(inputs.LatestMetrics);
            decimal competition = ComputeCompetitionScore(inputs.CompetitorsCount);

            decimal total = Round2(
                team * TeamWeight +
                market * MarketWeight +
                product * ProductWeight +
                traction * TractionWeight +
                competition * CompetitionWeight);

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

        // Spec: highest founder tier base, +15 if CEO+CTO+CMO present.
        // No experience = 30, Industry = 60, Serial = 80, Serial with exit = 90.
        private static decimal ComputeTeamScore(IReadOnlyList<MemberInput> members)
        {
            if (members.Count == 0)
            {
                return 0m;
            }

            FounderExperienceTier highest = FounderExperienceTier.NoExperience;
            foreach (MemberInput m in members)
            {
                if (m.Role != StartupRole.Founder)
                {
                    continue;
                }
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

        // Spec: Sub1B=20, 1-10B=60, 10B+=90, plus CAGR bump (+10 / +25).
        // Treat Tam as USD when classifying tiers (spec ranges are in $).
        private static decimal ComputeMarketScore(decimal? tam, decimal? cagr)
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

            return Clamp(tamBase + cagrBump);
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
        private static decimal ComputeProductScore(StartupStage stage, bool hasPatents)
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
            return Clamp(baseScore + patentBonus);
        }

        // Spec:
        // - No revenue but MAU > 0: 35
        // - MRR > 0, MoM 0–10%: 50
        // - MRR > 0, MoM 10–20%: 70
        // - MRR > ₽1M with MoM > 10%: 80
        // - MRR > ₽4M with MoM > 20%: 95
        private static decimal ComputeTractionScore(IReadOnlyDictionary<MetricType, decimal> latest)
        {
            decimal mrr = latest.TryGetValue(MetricType.Mrr, out decimal m) ? m : 0m;
            decimal mau = latest.TryGetValue(MetricType.Mau, out decimal a) ? a : 0m;
            decimal mom = latest.TryGetValue(MetricType.MomGrowth, out decimal g) ? g : 0m;

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
