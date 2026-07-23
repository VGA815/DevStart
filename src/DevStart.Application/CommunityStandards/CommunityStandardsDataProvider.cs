using DevStart.Application.Abstractions.Data;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.CommunityStandards
{
    internal sealed class CommunityStandardsDataProvider(IApplicationDbContext context) : ICommunityStandardsDataProvider
    {
        public async Task<Result<CommunityStandardsInputs>> GetInputsAsync(Guid startupId, CancellationToken cancellationToken)
        {
            // The whole entity is loaded rather than projected: SocialMediaLinks is a jsonb column behind
            // a value converter, so "has at least one link" can only be decided in memory.
            Startup? startup = await context.Startups
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == startupId, cancellationToken);

            if (startup is null)
            {
                return Result.Failure<CommunityStandardsInputs>(StartupErrors.NotFound(startupId));
            }

            var memberCounts = await context.StartupMembers
                .AsNoTracking()
                .Where(sm => sm.StartupId == startupId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Founders = g.Count(sm => sm.Role == StartupRole.Founder)
                })
                .SingleOrDefaultAsync(cancellationToken);

            // Mirrors ScoringDataProvider.BuildProductAsync — a product counts as articulated only when
            // both the value proposition and the differentiators are filled in.
            var product = await context.StartupProducts
                .AsNoTracking()
                .Where(p => p.StartupId == startupId)
                .Select(p => new { p.ValueProposition, p.Differentiators })
                .FirstOrDefaultAsync(cancellationToken);

            bool hasPitchDeck = await context.StartupDocumentFiles
                .AsNoTracking()
                .AnyAsync(d => d.StartupId == startupId && d.DocumentType == StartupDocumentType.Pitch, cancellationToken);

            int roadmapItemCount = await context.StartupRoadmapItems
                .AsNoTracking()
                .CountAsync(r => r.StartupId == startupId, cancellationToken);

            Dictionary<CommunityDocumentType, Guid> documents = await context.StartupCommunityDocuments
                .AsNoTracking()
                .Where(d => d.StartupId == startupId)
                .ToDictionaryAsync(d => d.Type, d => d.Id, cancellationToken);

            return new CommunityStandardsInputs(
                StartupId: startupId,
                HasDescription: !string.IsNullOrWhiteSpace(startup.Description)
                             && !string.IsNullOrWhiteSpace(startup.ShortDescription),
                HasLogo: startup.AvatarId is not null,
                HasLinks: !string.IsNullOrWhiteSpace(startup.Url)
                       || (startup.SocialMediaLinks?.Any(l => !string.IsNullOrWhiteSpace(l)) ?? false),
                HasArticulatedProduct: product is not null
                                    && !string.IsNullOrWhiteSpace(product.ValueProposition)
                                    && !string.IsNullOrWhiteSpace(product.Differentiators),
                MemberCount: memberCounts?.Total ?? 0,
                HasFounder: memberCounts?.Founders > 0,
                HasPitchDeck: hasPitchDeck,
                RoadmapItemCount: roadmapItemCount,
                Documents: documents);
        }
    }
}
