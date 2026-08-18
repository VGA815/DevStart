using DevStart.Application.Abstractions.BackgroundJobs;
using DevStart.Infrastructure.Authentication;
using DevStart.Infrastructure.DealDocuments;
using DevStart.Infrastructure.PatentRegistry;
using DevStart.Infrastructure.Valuation;
using Hangfire;

namespace DevStart.Infrastructure.BackgroundJobs
{
    internal sealed class HangfireBackgroundJobScheduler(IBackgroundJobClient client) : IBackgroundJobScheduler
    {
        public void EnqueueTermSheetGeneration(Guid dealId)
        {
            client.Enqueue<TermSheetGenerationJob>(j => j.GenerateAsync(dealId, CancellationToken.None));
        }

        public void EnqueueNewDeviceLoginEmail(
            string email, string? browser, string? os, string? ipAddress, DateTime occurredAtUtc)
        {
            client.Enqueue<NewDeviceLoginEmailJob>(
                j => j.SendAsync(email, browser, os, ipAddress, occurredAtUtc));
        }

        public void EnqueueMarketCapCollection()
        {
            client.Enqueue<MoexMarketCapCollectionJob>(j => j.RunAsync(CancellationToken.None));
        }

        public void EnqueueRevenueCollection()
        {
            client.Enqueue<GirBoRevenueCollectionJob>(j => j.RunAsync(CancellationToken.None));
        }

        public void EnqueuePatentRegistryImport()
        {
            client.Enqueue<PatentRegistryImportJob>(j => j.RunAsync(CancellationToken.None));
        }
    }
}
