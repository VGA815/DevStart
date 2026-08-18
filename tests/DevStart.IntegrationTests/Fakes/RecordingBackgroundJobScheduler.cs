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

        public ConcurrentQueue<BenchmarkCollector> BenchmarkCollections { get; } = new();

        public void EnqueueNewDeviceLoginEmail(
            string email, string? browser, string? os, string? ipAddress, DateTime occurredAtUtc)
            => NewDeviceLoginEmails.Enqueue((email, browser, os, ipAddress, occurredAtUtc));

        public void EnqueueMarketCapCollection() => BenchmarkCollections.Enqueue(BenchmarkCollector.MarketCap);

        public void EnqueueRevenueCollection() => BenchmarkCollections.Enqueue(BenchmarkCollector.Revenue);

        public ConcurrentQueue<DateTime> PatentRegistryImports { get; } = new();

        public void EnqueuePatentRegistryImport() => PatentRegistryImports.Enqueue(DateTime.UtcNow);
    }

    /// <summary>Which benchmark collector a test saw queued. Typed so an assertion cannot mistype it.</summary>
    internal enum BenchmarkCollector
    {
        MarketCap,
        Revenue,
    }
}
