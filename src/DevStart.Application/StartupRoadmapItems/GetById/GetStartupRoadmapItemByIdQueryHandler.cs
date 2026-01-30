using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupRoadmapItems;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupRoadmapItems.GetById
{
    internal sealed class GetStartupRoadmapItemByIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupRoadmapItemByIdQuery, StartupRoadmapItemResponse>
    {
        public async Task<Result<StartupRoadmapItemResponse>> Handle(GetStartupRoadmapItemByIdQuery query, CancellationToken cancellationToken)
        {
            StartupRoadmapItem? startupRoadmapItem = await context.StartupRoadmapItems
                .SingleOrDefaultAsync(sri => sri.Id == query.ItemId, cancellationToken);

            if (startupRoadmapItem == null)
            {
                return Result.Failure<StartupRoadmapItemResponse>(StartupRoadmapItemErrors.NotFound(query.ItemId));
            }

            StartupRoadmapItemResponse startupRoadmapItemResponse = new StartupRoadmapItemResponse()
            {
                Id = startupRoadmapItem.Id,
                CreatedAt = startupRoadmapItem.CreatedAt,
                Desription = startupRoadmapItem.Desription,
                StartupId = startupRoadmapItem.StartupId,
                StartupStage = startupRoadmapItem.StartupStage,
                Status = startupRoadmapItem.Status,
                TargetDate = startupRoadmapItem.TargetDate,
                Title = startupRoadmapItem.Title,
            };

            return startupRoadmapItemResponse;
        }
    }
}
