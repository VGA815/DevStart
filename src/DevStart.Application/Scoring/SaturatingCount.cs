namespace DevStart.Application.Scoring
{
    /// <summary>
    /// The one saturating ladder used wherever a score is driven by a count of worked-out records:
    /// competitor cards with an analysis, partnership records with an account of the arrangement. The
    /// shape is always the same — each record up to a saturation point is worth an equal slice of the
    /// ceiling, and past that point more records are worth nothing.
    ///
    /// It lives in one place on purpose. Two hand-written <c>switch</c> ladders side by side would
    /// eventually saturate at different counts or step unevenly, and the difference would read as a
    /// deliberate statement about competitors versus partners rather than as the accident it was
    /// (М3 in docs/scoring-inputs-plan.md).
    ///
    /// Why a count of worked-out records rather than the total: the total is something the startup
    /// controls by adding and deleting rows, so it must never be a driver. Adding a record can only
    /// help, deleting one can only hurt, and an empty record is worth exactly nothing.
    /// </summary>
    internal static class SaturatingCount
    {
        /// <summary>
        /// The slice of <paramref name="ceiling"/> earned by <paramref name="count"/> worked-out
        /// records, saturating at <paramref name="saturateAt"/>. Multiplication comes before division
        /// so the rungs are exact (30 → 10 / 20 / 30, never 9.999…).
        /// </summary>
        public static decimal Of(decimal ceiling, int count, int saturateAt) =>
            saturateAt <= 0 ? 0m : ceiling * Math.Clamp(count, 0, saturateAt) / saturateAt;

        /// <summary>
        /// The same ladder as a 0..1 signal, for callers that scale something else by it.
        /// </summary>
        public static decimal Share(int count, int saturateAt) => Of(1m, count, saturateAt);
    }
}
