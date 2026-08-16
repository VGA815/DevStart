namespace DevStart.Application.AccountDeletion
{
    /// <summary>
    /// Bound from the "AccountDeletion" configuration section.
    ///
    /// The legal documents promise erasure within 30 days of the request (offer §8.2, ст. 21 ФЗ-152),
    /// so the grace window has to leave room for the daily job — and for a job that misses a day —
    /// inside that promise. A week does; a month would not.
    /// </summary>
    public sealed class AccountDeletionOptions
    {
        /// <summary>
        /// Days between the request and the erasure, during which the user can still cancel and the
        /// account keeps working. Zero means erase on the next job run.
        /// </summary>
        public int GraceDays { get; set; } = 7;

        /// <summary>Safety net: the outer bound the promise is measured against, asserted in tests.</summary>
        public const int PromisedMaxDays = 30;

        public TimeSpan Grace => TimeSpan.FromDays(Math.Max(0, GraceDays));
    }
}
