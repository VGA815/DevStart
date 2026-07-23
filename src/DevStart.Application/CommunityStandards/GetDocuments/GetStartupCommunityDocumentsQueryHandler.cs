using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.CommunityStandards.GetDocuments
{
    internal sealed class GetStartupCommunityDocumentsQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetStartupCommunityDocumentsQuery, List<CommunityDocumentSummary>>
    {
        public async Task<Result<List<CommunityDocumentSummary>>> Handle(
            GetStartupCommunityDocumentsQuery query,
            CancellationToken cancellationToken)
        {
            bool isVisible = await PublicStartupVisibility.IsVisibleAsync(
                context, query.StartupId, dateTimeProvider.UtcNow, cancellationToken);

            if (!isVisible)
            {
                return Result.Failure<List<CommunityDocumentSummary>>(StartupErrors.NotFound(query.StartupId));
            }

            return await context.StartupCommunityDocuments
                .AsNoTracking()
                .Where(d => d.StartupId == query.StartupId)
                .OrderBy(d => d.Type)
                .Select(d => new CommunityDocumentSummary(d.Id, d.Type, d.Title, d.CreatedAt, d.UpdatedAt))
                .ToListAsync(cancellationToken);
        }
    }
}
