using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Application.Scoring;
using DevStart.Application.StartupEquity;
using DevStart.Application.StartupEquity.Vesting;
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
        IFoundingCapTableProvider capTableProvider,
        IVestingCalculator vestingCalculator,
        IFileStorage fileStorage,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider,
        IQueryHandler<ComputeStartupScoreQuery, ScoreResult> scoreHandler,
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

            // Background job has no user context — use the ungated compute path (the public
            // GetStartupScoreQuery would fail its Pro/member gate here).
            Result<ScoreResult> scoreResult = await scoreHandler.Handle(
                new ComputeStartupScoreQuery(startup.Id),
                cancellationToken);
            ScoreResult score = scoreResult.IsSuccess
                ? scoreResult.Value
                : new ScoreResult(0, 0, 0, 0, 0, 0, 0, 0, Array.Empty<string>(), dateTimeProvider.UtcNow);

            // Persist a valuation snapshot for history/backtesting and document provenance. Only when
            // scoring actually produced a valuation (methods present) — never store a fabricated 0/0.
            // Saved together with the DealDocument at step 8.
            if (scoreResult.IsSuccess && score.MethodsUsed.Count > 0)
            {
                string? breakdownJson = score.ValuationMethods is { Count: > 0 }
                    ? JsonSerializer.Serialize(score.ValuationMethods, ValuationSnapshotJson.Options)
                    : null;

                context.StartupValuationSnapshots.Add(StartupValuationSnapshot.Create(
                    startup.Id,
                    score.TotalScore, score.TeamScore, score.MarketScore, score.ProductScore,
                    score.TractionScore, score.CompetitionScore,
                    score.ValuationLow, score.ValuationHigh, score.ValuationPoint,
                    string.Join(",", score.MethodsUsed),
                    breakdownJson,
                    score.MethodologyVersion,
                    score.CalculatedAt));
            }

            // 3. Resolve the startup's founding cap table (explicit per-founder equity + vesting when
            //    set; equal-split + default ESOP fallback otherwise) and map it into calculator inputs,
            //    resolving each holder's vested fraction as of now.
            DateTime asOf = dateTimeProvider.UtcNow;
            IReadOnlyList<FoundingCapTableHolder> foundingHolders =
                await capTableProvider.GetEffectiveHoldersAsync(deal.StartupId, cancellationToken);
            List<EquityHolderInput> holdersBefore = foundingHolders
                .Select(h => new EquityHolderInput(
                    h.ProfileId,
                    h.Name,
                    h.HolderType.ToString(),
                    h.EquityPercentage,
                    vestingCalculator.VestedFraction(h.VestingStartDate, h.VestingMonths, h.CliffMonths, asOf)))
                .ToList();

            // 4. Compute cap table
            CapTableResult capTable = capTableCalculator.Compute(deal, holdersBefore);

            // 5. Render markdown
            string markdown = await termSheetGenerator.RenderAsync(
                deal, startup, score, capTable, foundingHolders, asOf, cancellationToken);

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
    }
}
