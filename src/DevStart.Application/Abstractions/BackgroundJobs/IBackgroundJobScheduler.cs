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
    }
}
