using DevStart.SharedKernel;

namespace DevStart.Domain.StartupMetrics
{
    public static class StartupMetricErrors
    {
        public static Error NotFound(Guid metricId) => Error.NotFound(
            "StartupMetrics.NotFound",
            $"The startup metric with Id = '{metricId}' was not found");
        public static readonly Error NotFoundByStartupId = Error.NotFound(
            "StartupMetrics.NotFoundByStartupId",
            $"The startup metric with specified startupId was not found");
    }
}
