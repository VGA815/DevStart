using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupRoadmapItems.GetAllByStartupId
{
    internal sealed class GetStartupRoadmapItemsByStartupIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupRoadmapItemsByStartupIdQuery, List<StartupRoadmapItemResponse>>
    {
        public async Task<Result<List<StartupRoadmapItemResponse>>> Handle(GetStartupRoadmapItemsByStartupIdQuery query, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(sri => sri.Id == query.StartupId, cancellationToken))
            {
                return Result.Failure<List<StartupRoadmapItemResponse>>(StartupErrors.NotFound(query.StartupId));
            }

            List<StartupRoadmapItemResponse> startupRoadmapItemResponses = await context.StartupRoadmapItems
                .Where(sri => sri.StartupId == query.StartupId)
                .Select(sri => new StartupRoadmapItemResponse
                {
                    StartupId = sri.StartupId,
                    CreatedAt = DateTime.UtcNow,
                    Desription = sri.Desription,
                    Id = sri.Id,
                    StartupStage = sri.StartupStage,
                    Status = sri.Status,
                    TargetDate = DateTime.UtcNow,
                    Title = sri.Title,
                })
                .ToListAsync(cancellationToken);

            return startupRoadmapItemResponses;
        }
    }
}
