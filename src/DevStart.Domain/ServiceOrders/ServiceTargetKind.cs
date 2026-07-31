namespace DevStart.Domain.ServiceOrders
{
    /// <summary>
    /// What a one-time service order points at. A service is bought *for* something — a scoring
    /// report is about one startup, a term sheet about one deal — so the entitlement it grants can be
    /// scoped instead of unlocking the feature account-wide.
    /// </summary>
    public enum ServiceTargetKind
    {
        None = 0,
        Startup = 1,
        Deal = 2,
    }

    public static class ServiceTargets
    {
        /// <summary>The kind of entity a <see cref="ServiceType"/> must be bought for.</summary>
        public static ServiceTargetKind KindOf(ServiceType serviceType) => serviceType switch
        {
            ServiceType.ScoringReport => ServiceTargetKind.Startup,
            ServiceType.TermSheet => ServiceTargetKind.Deal,
            ServiceType.Promotion => ServiceTargetKind.Startup,
            _ => ServiceTargetKind.None,
        };

        public static bool RequiresTarget(ServiceType serviceType) => KindOf(serviceType) != ServiceTargetKind.None;
    }
}
