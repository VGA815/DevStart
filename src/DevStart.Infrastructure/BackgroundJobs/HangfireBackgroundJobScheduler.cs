using DevStart.Application.Abstractions.BackgroundJobs;
using DevStart.Infrastructure.DealDocuments;
using Hangfire;

namespace DevStart.Infrastructure.BackgroundJobs
{
    internal sealed class HangfireBackgroundJobScheduler(IBackgroundJobClient client) : IBackgroundJobScheduler
    {
        public void EnqueueTermSheetGeneration(Guid dealId)
        {
            client.Enqueue<TermSheetGenerationJob>(j => j.GenerateAsync(dealId, CancellationToken.None));
        }
    }
}
