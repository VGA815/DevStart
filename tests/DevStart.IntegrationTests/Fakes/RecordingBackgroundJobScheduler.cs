using System.Collections.Concurrent;
using DevStart.Application.Abstractions.BackgroundJobs;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>Replaces the Hangfire-backed scheduler. Records enqueued jobs instead of running them, so
    /// tests stay deterministic (no background work fires mid-assertion) and can verify a job was queued.</summary>
    internal sealed class RecordingBackgroundJobScheduler : IBackgroundJobScheduler
    {
        public ConcurrentQueue<Guid> TermSheetGenerations { get; } = new();

        public void EnqueueTermSheetGeneration(Guid dealId) => TermSheetGenerations.Enqueue(dealId);
    }
}
