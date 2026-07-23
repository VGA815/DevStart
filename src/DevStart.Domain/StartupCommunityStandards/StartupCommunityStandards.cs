using DevStart.SharedKernel;

namespace DevStart.Domain.StartupCommunityStandards
{
    /// <summary>
    /// Denormalized checklist result for a startup, one row per startup. Exists purely so the public
    /// catalog can show a badge and filter by level without evaluating twelve checks per row.
    /// The live read path (<c>api/startups/{id}/community</c>) always recomputes — this row is the
    /// projection, not the source of truth, and may lag by up to a day for signals that are only
    /// swept by the refresh job.
    /// </summary>
    public sealed class StartupCommunityStandards : Entity
    {
        public Guid StartupId { get; set; }
        public int CompletedCount { get; set; }
        public int TotalCount { get; set; }
        public CommunityStandardsLevel Level { get; set; }
        public DateTime ComputedAt { get; set; }

        public StartupCommunityStandards() { }

        public static StartupCommunityStandards Create(
            Guid startupId,
            int completedCount,
            int totalCount,
            CommunityStandardsLevel level,
            DateTime computedAt)
            => new()
            {
                StartupId      = startupId,
                CompletedCount = completedCount,
                TotalCount     = totalCount,
                Level          = level,
                ComputedAt     = computedAt
            };

        public void Update(int completedCount, int totalCount, CommunityStandardsLevel level, DateTime computedAt)
        {
            CompletedCount = completedCount;
            TotalCount     = totalCount;
            Level          = level;
            ComputedAt     = computedAt;
        }
    }
}
