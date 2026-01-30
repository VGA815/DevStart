using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMetrics.GetAllByStartupId
{
    internal sealed class GetStartupMetricsByStartupIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupMetricsByStartupIdQuery, List<StartupMetricResponse>>
    {
        public async Task<Result<List<StartupMetricResponse>>> Handle(GetStartupMetricsByStartupIdQuery query, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(sm => sm.Id == query.StartupId, cancellationToken))
            {
                return Result.Failure<List<StartupMetricResponse>>(StartupErrors.NotFound(query.StartupId));
            }

            List<StartupMetricResponse> startupMetricResponses = await context.StartupMetrics
                .Where(sm => sm.StartupId == query.StartupId)
                .Select(sm => new StartupMetricResponse
                {
                    CreatedAt = sm.CreatedAt,
                    Id = sm.Id,
                    MetricType = sm.MetricType,
                    StartupId = sm.StartupId,
                    Value = sm.Value,
                })
                .ToListAsync(cancellationToken);

            return startupMetricResponses;
        }
    }
}
