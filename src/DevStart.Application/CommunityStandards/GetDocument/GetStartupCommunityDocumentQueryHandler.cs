using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.CommunityStandards.GetDocument
{
    internal sealed class GetStartupCommunityDocumentQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetStartupCommunityDocumentQuery, CommunityDocumentResponse>
    {
        public async Task<Result<CommunityDocumentResponse>> Handle(
            GetStartupCommunityDocumentQuery query,
            CancellationToken cancellationToken)
        {
            bool isVisible = await PublicStartupVisibility.IsVisibleAsync(
                context, query.StartupId, dateTimeProvider.UtcNow, cancellationToken);

            if (!isVisible)
            {
                return Result.Failure<CommunityDocumentResponse>(StartupErrors.NotFound(query.StartupId));
            }

            CommunityDocumentResponse? document = await context.StartupCommunityDocuments
                .AsNoTracking()
                .Where(d => d.StartupId == query.StartupId && d.Type == query.Type)
                .Select(d => new CommunityDocumentResponse(
                    d.Id, d.StartupId, d.Type, d.Title, d.Content, d.CreatedAt, d.UpdatedAt))
                .SingleOrDefaultAsync(cancellationToken);

            return document is null
                ? Result.Failure<CommunityDocumentResponse>(
                    StartupCommunityDocumentErrors.NotFound(query.StartupId, query.Type))
                : document;
        }
    }
}
