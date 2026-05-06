using DevStart.Domain.StartupMetrics;

namespace DevStart.Application.Subscriptions
{
    /// <summary>
    /// MetricType values that are gated behind a Pro subscription for non-member viewers.
    /// Members of the startup (Founder/Administration/Member) always see all metrics.
    /// </summary>
    public static class PremiumMetrics
    {
        public static readonly IReadOnlySet<MetricType> Types = new HashSet<MetricType>
        {
            MetricType.Mrr,
            MetricType.Mau,
            MetricType.MomGrowth,
            MetricType.Lvt,
        };

        public static bool IsPremium(MetricType type) => Types.Contains(type);
    }
}
