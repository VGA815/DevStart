using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMetrics;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMetrics.GetById
{
    internal sealed class FetchStartupMetricByIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<FetchStartupMetricByIdQuery, StartupMetricResponse>
    {
        public async Task<Result<StartupMetricResponse>> Handle(FetchStartupMetricByIdQuery query, CancellationToken cancellationToken)
        {
            StartupMetric? startupMetric = await context.StartupMetrics
                .AsNoTracking()
                .SingleOrDefaultAsync(sm => sm.Id == query.MetricId, cancellationToken);

            if (startupMetric == null)
            {
                return Result.Failure<StartupMetricResponse>(StartupMetricErrors.NotFound(query.MetricId));
            }

            return new StartupMetricResponse()
            {
                CreatedAt = startupMetric.CreatedAt,
                Id = startupMetric.Id,
                MetricType = startupMetric.MetricType,
                StartupId = startupMetric.StartupId,
                Value = startupMetric.Value,
            };
        }
    }
}
