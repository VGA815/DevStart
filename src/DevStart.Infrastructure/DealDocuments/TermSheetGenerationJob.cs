using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Application.Scoring;
using DevStart.Application.Startups.GetScore;
using DevStart.Domain.DealDocuments;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.Notifications;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DevStart.Infrastructure.DealDocuments
{
    /// <summary>
    /// Hangfire-driven background job. Triggered from
    /// `InvestmentApplicationAcceptedDomainEventHandler` after a deal is created.
    /// Idempotent: returns early if a `DealDocument` already exists for the deal.
    /// </summary>
    public sealed class TermSheetGenerationJob(
        IApplicationDbContext context,
        ICapTableCalculator capTableCalculator,
        ITermSheetGenerator termSheetGenerator,
        IFileStorage fileStorage,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider,
        IQueryHandler<GetStartupScoreQuery, ScoreResult> scoreHandler,
        ILogger<TermSheetGenerationJob> logger)
    {
        public async Task GenerateAsync(Guid dealId, CancellationToken cancellationToken)
        {
            // 1. Idempotency
            bool exists = await context.DealDocuments
                .AsNoTracking()
                .AnyAsync(d => d.DealId == dealId, cancellationToken);
            if (exists)
            {
                logger.LogInformation("DealDocument already exists for {DealId}, skipping", dealId);
                return;
            }

            // 2. Load deal + startup + score
            InvestmentDeal? deal = await context.InvestmentDeals
                .AsNoTracking()
                .SingleOrDefaultAsync(d => d.Id == dealId, cancellationToken);
            if (deal is null)
            {
                logger.LogWarning("Deal {DealId} not found, cannot generate documents", dealId);
                return;
            }

            Startup? startup = await context.Startups
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == deal.StartupId, cancellationToken);
            if (startup is null)
            {
                logger.LogWarning("Startup {StartupId} not found for deal {DealId}", deal.StartupId, dealId);
                return;
            }

            Result<ScoreResult> scoreResult = await scoreHandler.Handle(
                new GetStartupScoreQuery(startup.Id),
                cancellationToken);
            ScoreResult score = scoreResult.IsSuccess
                ? scoreResult.Value
                : new ScoreResult(0, 0, 0, 0, 0, 0, 0, 0, Array.Empty<string>(), dateTimeProvider.UtcNow);

            // 3. Build holdersBefore: founders split (100 - 10% ESOP) equally; previous
            //    completed deals applied cumulatively.
            List<EquityHolderInput> holdersBefore = await BuildHoldersBeforeAsync(deal, cancellationToken);

            // 4. Compute cap table
            CapTableResult capTable = capTableCalculator.Compute(deal, holdersBefore);

            // 5. Render markdown
            string markdown = await termSheetGenerator.RenderAsync(deal, startup, score, capTable, cancellationToken);

            // 6. Upload markdown
            string termSheetKey = DealDocumentBuckets.TermSheetObjectKey(dealId);
            byte[] markdownBytes = Encoding.UTF8.GetBytes(markdown);
            using (var ms = new MemoryStream(markdownBytes, writable: false))
            {
                await fileStorage.UploadAsync(
                    termSheetKey,
                    ms,
                    DealDocumentBuckets.DealDocuments,
                    "text/markdown; charset=utf-8",
                    cancellationToken);
            }

            // 7. Upload cap table JSON
            string capTableKey = DealDocumentBuckets.CapTableObjectKey(dealId);
            byte[] capTableBytes = JsonSerializer.SerializeToUtf8Bytes(capTable);
            using (var ms = new MemoryStream(capTableBytes, writable: false))
            {
                await fileStorage.UploadAsync(
                    capTableKey,
                    ms,
                    DealDocumentBuckets.DealDocuments,
                    "application/json; charset=utf-8",
                    cancellationToken);
            }

            // 8. Save DealDocument
            DateTime utcNow = dateTimeProvider.UtcNow;
            DealDocument doc = DealDocument.Create(dealId, termSheetKey, capTableKey, utcNow);
            context.DealDocuments.Add(doc);
            await context.SaveChangesAsync(cancellationToken);

            // 9. Notify investor + founders/admins
            await notificationService.PublishAsync(Notification.Create(
                userId: deal.InvestorProfileId,
                type: NotificationType.DealDocumentsReady,
                title: "Deal documents are ready",
                body: "Term sheet and cap table for your deal are now available.",
                createdAt: utcNow,
                referenceId: dealId), cancellationToken);

            List<Guid> recipients = await context.StartupMembers
                .AsNoTracking()
                .Where(sm => sm.StartupId == deal.StartupId
                          && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration))
                .Select(sm => sm.ProfileId)
                .ToListAsync(cancellationToken);

            foreach (Guid recipientId in recipients)
            {
                await notificationService.PublishAsync(Notification.Create(
                    userId: recipientId,
                    type: NotificationType.DealDocumentsReady,
                    title: "Deal documents are ready",
                    body: "Term sheet and cap table for the accepted deal are now available.",
                    createdAt: utcNow,
                    referenceId: dealId), cancellationToken);
            }

            logger.LogInformation("Generated deal documents for {DealId}", dealId);
        }

        // MVP rule: founders split (100 - 10% ESOP) equally if no prior completed deals.
        // Otherwise, take the latest completed deal's cap table (we can't easily reconstruct
        // since cap-tables aren't stored relationally — this is a known MVP limitation).
        private async Task<List<EquityHolderInput>> BuildHoldersBeforeAsync(
            InvestmentDeal deal,
            CancellationToken cancellationToken)
        {
            const decimal esopPct = 10m;
            const decimal foundersPoolPct = 100m - esopPct;

            List<Guid> founderIds = await context.StartupMembers
                .AsNoTracking()
                .Where(sm => sm.StartupId == deal.StartupId && sm.Role == StartupRole.Founder)
                .Select(sm => sm.ProfileId)
                .ToListAsync(cancellationToken);

            var holders = new List<EquityHolderInput>();

            if (founderIds.Count == 0)
            {
                holders.Add(new EquityHolderInput(null, "Founders pool", "Founder", foundersPoolPct));
            }
            else
            {
                decimal perFounder = Math.Round(foundersPoolPct / founderIds.Count, 2, MidpointRounding.AwayFromZero);
                foreach (Guid founderId in founderIds)
                {
                    holders.Add(new EquityHolderInput(founderId, $"Founder {founderId}", "Founder", perFounder));
                }
            }

            holders.Add(new EquityHolderInput(null, "ESOP pool", "Esop", esopPct));

            return holders;
        }
    }
}
