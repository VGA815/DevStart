using System.Collections.Concurrent;
using DevStart.Application.Abstractions.BackgroundJobs;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>Replaces the Hangfire-backed scheduler. Records enqueued jobs instead of running them, so
    /// tests stay deterministic (no background work fires mid-assertion) and can verify a job was queued.</summary>
    internal sealed class RecordingBackgroundJobScheduler : IBackgroundJobScheduler
    {
        public ConcurrentQueue<Guid> TermSheetGenerations { get; } = new();

        public ConcurrentQueue<(string Email, string? Browser, string? Os, string? IpAddress, DateTime OccurredAtUtc)>
            NewDeviceLoginEmails { get; } = new();

        public void EnqueueTermSheetGeneration(Guid dealId) => TermSheetGenerations.Enqueue(dealId);

        public void EnqueueNewDeviceLoginEmail(
            string email, string? browser, string? os, string? ipAddress, DateTime occurredAtUtc)
            => NewDeviceLoginEmails.Enqueue((email, browser, os, ipAddress, occurredAtUtc));
    }
}
