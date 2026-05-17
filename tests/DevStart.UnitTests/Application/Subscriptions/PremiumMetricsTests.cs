using DevStart.Application.Subscriptions;
using DevStart.Domain.StartupMetrics;
using Shouldly;

namespace DevStart.UnitTests.Application.Subscriptions;

public sealed class PremiumMetricsTests
{
    [Fact]
    public void Types_ShouldContainExactlyPremiumMetrics()
    {
        PremiumMetrics.Types.ShouldBe([
            MetricType.Mrr,
            MetricType.Mau,
            MetricType.MomGrowth,
            MetricType.Lvt
        ], ignoreOrder: true);
    }

    [Theory]
    [InlineData(MetricType.Mrr)]
    [InlineData(MetricType.Mau)]
    [InlineData(MetricType.MomGrowth)]
    [InlineData(MetricType.Lvt)]
    public void IsPremium_ShouldReturnTrue_ForPremiumMetric(MetricType metricType)
    {
        PremiumMetrics.IsPremium(metricType).ShouldBeTrue();
    }

    [Theory]
    [InlineData(MetricType.Users)]
    [InlineData(MetricType.Revenue)]
    [InlineData(MetricType.Cac)]
    [InlineData(MetricType.GrowthRate)]
    [InlineData(MetricType.Etc)]
    public void IsPremium_ShouldReturnFalse_ForNonPremiumMetric(MetricType metricType)
    {
        PremiumMetrics.IsPremium(metricType).ShouldBeFalse();
    }
}
