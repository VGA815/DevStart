using DevStart.Application.Abstractions.Data;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.StartupRoadmapItems;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Scoring
{
    internal sealed class ScoringDataProvider(IApplicationDbContext context) : IScoringDataProvider
    {
        // Only these metric types feed scoring. Primary signals (Mrr/Mau/MomGrowth) plus the
        // fallback proxies (Revenue/Users/GrowthRate) resolved in BuildTraction.
        private static readonly MetricType[] ConsumedMetricTypes =
        [
            MetricType.Mrr, MetricType.Mau, MetricType.MomGrowth,
            MetricType.Revenue, MetricType.Users, MetricType.GrowthRate
        ];

        public async Task<Result<ScoringInputs>> GetInputsAsync(Guid startupId, CancellationToken cancellationToken)
        {
            Startup? startup = await context.Startups
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == startupId, cancellationToken);

            if (startup is null)
            {
                return Result.Failure<ScoringInputs>(StartupErrors.NotFound(startupId));
            }

            List<MemberInput> members = await context.StartupMembers
                .AsNoTracking()
                .Where(sm => sm.StartupId == startupId)
                .Select(sm => new MemberInput(
                    sm.ProfileId,
                    sm.Role,
                    sm.Position,
                    sm.YearsOfExperience,
                    sm.HasPriorExit,
                    sm.PreviousStartupsCount))
                .ToListAsync(cancellationToken);

            CompetitorSignals competitors = await BuildCompetitorsAsync(startupId, cancellationToken);
            TractionSignals traction = await BuildTractionAsync(startupId, cancellationToken);
            ProductSignals product = await BuildProductAsync(startupId, cancellationToken);
            RoadmapSignals roadmap = await BuildRoadmapAsync(startupId, cancellationToken);

            ScoringInputs inputs = new(
                StartupId: startup.Id,
                Stage: startup.Stage,
                Tam: startup.Tam,
                Sam: startup.Sam,
                Som: startup.Som,
                MarketGrowthRate: startup.MarketGrowthRate,
                HasPatents: startup.HasPatents,
                Competitors: competitors,
                Members: members,
                Traction: traction,
                Product: product,
                Roadmap: roadmap,
                Industry: startup.Industry,
                TargetRoundAmount: startup.TargetRoundAmount,
                HasStrategicPartnerships: startup.HasStrategicPartnerships);

            return inputs;
        }

        // A card counts as "well documented" when it carries an actual analysis: a website plus at
        // least one of strengths/weaknesses vs us. The total is carried for transparency only — the
        // score is driven by the documented count, so adding an empty card is worth nothing and
        // deleting one cannot raise the score (docs/scoring-methodology.md).
        private async Task<CompetitorSignals> BuildCompetitorsAsync(Guid startupId, CancellationToken cancellationToken)
        {
            var cards = await context.StartupCompetitors
                .AsNoTracking()
                .Where(c => c.StartupId == startupId)
                .Select(c => new { c.Website, c.StrengthsVsUs, c.WeaknessesVsUs })
                .ToListAsync(cancellationToken);

            int wellDocumented = cards.Count(c =>
                !string.IsNullOrWhiteSpace(c.Website)
                && (!string.IsNullOrWhiteSpace(c.StrengthsVsUs) || !string.IsNullOrWhiteSpace(c.WeaknessesVsUs)));

            return new CompetitorSignals(cards.Count, wellDocumented);
        }

        // Latest snapshot per consumed metric type. Only the consumed types are pulled (not the whole
        // history); the per-type "latest by CreatedAt" pick is done in memory over that small set so it
        // translates identically on PostgreSQL and the in-memory test provider.
        private async Task<TractionSignals> BuildTractionAsync(Guid startupId, CancellationToken cancellationToken)
        {
            List<MetricSnapshot> snapshots = await context.StartupMetrics
                .AsNoTracking()
                .Where(m => m.StartupId == startupId && ConsumedMetricTypes.Contains(m.MetricType))
                .Select(m => new MetricSnapshot(m.MetricType, m.Value, m.CreatedAt))
                .ToListAsync(cancellationToken);

            Dictionary<MetricType, decimal> latest = snapshots
                .GroupBy(s => s.Type)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(s => s.CreatedAt).First().Value);

            // Metric fallback: a startup that tracks Revenue/Users/GrowthRate instead of the dedicated
            // MRR-family still gets a traction signal. Revenue is used only as an MRR proxy when no Mrr
            // metric exists; the proxy is flagged so it feeds the traction score but never the
            // valuation's ARR anchor (Revenue's period is undefined — see TractionSignals).
            decimal? mrr = Pick(latest, MetricType.Mrr, MetricType.Revenue);
            decimal? mau = Pick(latest, MetricType.Mau, MetricType.Users);
            decimal? mom = Pick(latest, MetricType.MomGrowth, MetricType.GrowthRate);
            bool mrrIsProxy = !latest.ContainsKey(MetricType.Mrr) && latest.ContainsKey(MetricType.Revenue);

            return TractionSignals.From(mrr, mau, mom, mrrIsProxy);
        }

        private static decimal? Pick(IReadOnlyDictionary<MetricType, decimal> latest, MetricType primary, MetricType fallback)
        {
            if (latest.TryGetValue(primary, out decimal p))
            {
                return p;
            }
            return latest.TryGetValue(fallback, out decimal f) ? f : null;
        }

        private async Task<ProductSignals> BuildProductAsync(Guid startupId, CancellationToken cancellationToken)
        {
            var product = await context.StartupProducts
                .AsNoTracking()
                .Where(p => p.StartupId == startupId)
                .Select(p => new { p.ValueProposition, p.Differentiators })
                .FirstOrDefaultAsync(cancellationToken);

            bool articulated = product is not null
                && !string.IsNullOrWhiteSpace(product.ValueProposition)
                && !string.IsNullOrWhiteSpace(product.Differentiators);

            return new ProductSignals(articulated);
        }

        private async Task<RoadmapSignals> BuildRoadmapAsync(Guid startupId, CancellationToken cancellationToken)
        {
            List<RoadmapItemStatus> statuses = await context.StartupRoadmapItems
                .AsNoTracking()
                .Where(r => r.StartupId == startupId)
                .Select(r => r.Status)
                .ToListAsync(cancellationToken);

            int doneCount = statuses.Count(s => s == RoadmapItemStatus.Done);
            return new RoadmapSignals(statuses.Count, doneCount);
        }

        private readonly record struct MetricSnapshot(MetricType Type, decimal Value, DateTime CreatedAt);
    }
}
