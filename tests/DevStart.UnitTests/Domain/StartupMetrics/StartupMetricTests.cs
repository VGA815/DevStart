using DevStart.Domain.StartupMetrics;
using Shouldly;

namespace DevStart.UnitTests.Domain.StartupMetrics;

public sealed class StartupMetricTests
{
    [Fact]
    public void Create_ShouldInitializeStartupMetric()
    {
        Guid startupId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        StartupMetric metric = StartupMetric.Create(startupId, MetricType.Mrr, 100_000m, createdAt);

        metric.Id.ShouldNotBe(Guid.Empty);
        metric.StartupId.ShouldBe(startupId);
        metric.MetricType.ShouldBe(MetricType.Mrr);
        metric.Value.ShouldBe(100_000m);
        metric.CreatedAt.ShouldBe(createdAt);
    }
}
