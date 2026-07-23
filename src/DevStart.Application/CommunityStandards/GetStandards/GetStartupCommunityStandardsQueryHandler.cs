using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.CommunityStandards.ComputeStandards;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;

namespace DevStart.Application.CommunityStandards.GetStandards
{
    internal sealed class GetStartupCommunityStandardsQueryHandler(
        IApplicationDbContext context,
        IQueryHandler<ComputeStartupCommunityStandardsQuery, CommunityStandardsResult> computeHandler,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetStartupCommunityStandardsQuery, CommunityStandardsResult>
    {
        public async Task<Result<CommunityStandardsResult>> Handle(
            GetStartupCommunityStandardsQuery query,
            CancellationToken cancellationToken)
        {
            // Visibility is re-checked on every request; the evaluation behind it is what gets cached.
            bool isVisible = await PublicStartupVisibility.IsVisibleAsync(
                context, query.StartupId, dateTimeProvider.UtcNow, cancellationToken);

            if (!isVisible)
            {
                return Result.Failure<CommunityStandardsResult>(StartupErrors.NotFound(query.StartupId));
            }

            return await computeHandler.Handle(
                new ComputeStartupCommunityStandardsQuery(query.StartupId), cancellationToken);
        }
    }
}
