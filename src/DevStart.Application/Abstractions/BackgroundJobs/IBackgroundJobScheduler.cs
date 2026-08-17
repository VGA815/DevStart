namespace DevStart.Application.Abstractions.BackgroundJobs
{
    /// <summary>
    /// Application-layer abstraction over the background job framework. The Infrastructure
    /// implementation forwards calls to Hangfire's IBackgroundJobClient. Methods are kept
    /// purpose-specific so Application code never depends on Hangfire types.
    /// </summary>
    public interface IBackgroundJobScheduler
    {
        void EnqueueTermSheetGeneration(Guid dealId);

        /// <summary>
        /// Queued rather than sent inline because domain events are dispatched inside SaveChangesAsync
        /// — an inline SMTP call would put mail-server latency on the login request itself.
        /// </summary>
        void EnqueueNewDeviceLoginEmail(
            string email, string? browser, string? os, string? ipAddress, DateTime occurredAtUtc);

        /// <summary>
        /// Runs the MOEX market-cap collection now instead of waiting for the quarter. Needed twice:
        /// before the first scheduled run ever fires, and whenever a ticker list or an outage needs
        /// re-checking without a four-month wait.
        /// </summary>
        void EnqueueMarketCapCollection();

        /// <summary>Runs the ГИР БО revenue collection now. Same reasons as the market-cap run.</summary>
        void EnqueueRevenueCollection();
    }
}
