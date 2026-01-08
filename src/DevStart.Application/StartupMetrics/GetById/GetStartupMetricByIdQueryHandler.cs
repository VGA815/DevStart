using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMetrics;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMetrics.GetById
{
    internal sealed class GetStartupMetricByIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupMetricByIdQuery, StartupMetricResponse>
    {
        public async Task<Result<StartupMetricResponse>> Handle(GetStartupMetricByIdQuery query, CancellationToken cancellationToken)
        {
            StartupMetric? startupMetric = await context.StartupMetrics
                .SingleOrDefaultAsync(sm => sm.Id == query.MetricId);

            if (startupMetric == null)
            {
                return Result.Failure<StartupMetricResponse>(StartupMetricErrors.NotFound(query.MetricId));
            }

            StartupMetricResponse startupMetricResponse = new StartupMetricResponse()
            {
                CreatedAt = startupMetric.CreatedAt,
                Id = startupMetric.Id,
                MetricType = startupMetric.MetricType,
                StartupId = startupMetric.Id,
                Value = startupMetric.Value,
            };

            return startupMetricResponse;
        }
    }
}
