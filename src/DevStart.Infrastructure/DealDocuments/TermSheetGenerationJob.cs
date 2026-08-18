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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DevStart.Infrastructure.DealDocuments
{
    /// <summary>
    /// Hangfire-driven background job. Triggered from
    /// `InvestmentApplicationAcceptedDomainEventHandler` after a deal is created.
    /// Idempotent: returns early if a complete `DealDocument` already exists for the deal.
    /// <para>
    /// Both renderings happen before the first upload. A failure while rendering therefore leaves
    /// nothing behind — no objects in storage, no row — and Hangfire retries from a clean slate,
    /// with `JobFailureAlertFilter` raising an alert once the retries are exhausted. The cost of
    /// that ordering is accepted knowingly: a systematically broken PDF renderer withholds the
    /// markdown too. The alternative, storing the row without a PDF, would create a
    /// "document exists, PDF missing" state that every reader would have to handle forever.
    /// </para>
    /// </summary>
    public sealed class TermSheetGenerationJob(
        IApplicationDbContext context,
        ICapTableCalculator capTableCalculator,
        ITermSheetComposer termSheetComposer,
        ITermSheetMarkdownRenderer markdownRenderer,
        ITermSheetPdfRenderer pdfRenderer,
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
            // 1. Idempotency. A complete set is never regenerated. A row written before PDF
            //    generation existed carries an empty PDF key and is the one case worth revisiting:
            //    since the job stops at the sight of a row, nothing else ever would.
            DealDocument? existing = await context.DealDocuments
                .SingleOrDefaultAsync(d => d.DealId == dealId, cancellationToken);
            if (existing is { HasPdf: true })
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
                : ScoreResult.InsufficientData(dateTimeProvider.UtcNow);

            // Persist a valuation snapshot for history/backtesting and document provenance. Only when
            // scoring actually produced a valuation (methods present) — never store a fabricated 0/0.
            // Saved together with the DealDocument at step 8.
            if (scoreResult.IsSuccess && score.MethodsUsed.Count > 0 && score.TotalScore is { } totalScore)
            {
                string? breakdownJson = score.ValuationMethods is { Count: > 0 }
                    ? JsonSerializer.Serialize(score.ValuationMethods, ValuationSnapshotJson.Options)
                    : null;

                context.StartupValuationSnapshots.Add(StartupValuationSnapshot.Create(
                    startup.Id,
                    totalScore, score.TeamScore, score.MarketScore, score.ProductScore,
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

            // 5. Compose the structural model, then render it
            TermSheetModel model = termSheetComposer.Compose(
                deal, startup, score, capTable, foundingHolders, asOf);

            string markdown = await markdownRenderer.RenderAsync(model, cancellationToken);
            byte[] markdownBytes = Encoding.UTF8.GetBytes(markdown);

            // 6. Render the PDF — before anything is uploaded, so that a rendering failure leaves no
            //    partial state anywhere.
            byte[] pdfBytes = pdfRenderer.Render(model);
            string pdfSha256 = Convert.ToHexStringLower(SHA256.HashData(pdfBytes));

            // 7. Upload all three objects
            string termSheetKey = DealDocumentBuckets.TermSheetObjectKey(dealId);
            string pdfKey = DealDocumentBuckets.TermSheetPdfObjectKey(dealId);
            string capTableKey = DealDocumentBuckets.CapTableObjectKey(dealId);
            byte[] capTableBytes = JsonSerializer.SerializeToUtf8Bytes(capTable);

            await UploadAsync(termSheetKey, markdownBytes, "text/markdown; charset=utf-8", cancellationToken);
            await UploadAsync(pdfKey, pdfBytes, "application/pdf", cancellationToken);
            await UploadAsync(capTableKey, capTableBytes, "application/json; charset=utf-8", cancellationToken);

            // 8. Save the DealDocument. A pre-PDF row is completed in place; there is never a second
            //    row for a deal, and a complete set is never rewritten.
            DateTime utcNow = dateTimeProvider.UtcNow;
            bool isNewDocument = existing is null;
            if (existing is null)
            {
                context.DealDocuments.Add(
                    DealDocument.Create(dealId, termSheetKey, pdfKey, pdfSha256, capTableKey, utcNow));
            }
            else
            {
                existing.AttachPdf(pdfKey, pdfSha256, utcNow);
                logger.LogInformation("Backfilled the PDF for the existing DealDocument of {DealId}", dealId);
            }

            await context.SaveChangesAsync(cancellationToken);

            // 9. Notify investor + founders/admins. A backfill adds a file to a set the recipients
            //    were already told about, so it does not announce itself a second time.
            if (!isNewDocument)
            {
                // The backfill logged what it actually did a few lines up; saying "generated deal
                // documents" here would claim a full set was produced when only a PDF was added.
                return;
            }

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

            await notificationService.PublishManyAsync(
                [.. recipients.Select(recipientId => Notification.Create(
                    userId: recipientId,
                    type: NotificationType.DealDocumentsReady,
                    title: "Deal documents are ready",
                    body: "Term sheet and cap table for the accepted deal are now available.",
                    createdAt: utcNow,
                    referenceId: dealId))],
                cancellationToken);

            logger.LogInformation("Generated deal documents for {DealId}", dealId);
        }

        private async Task UploadAsync(
            string objectKey,
            byte[] content,
            string contentType,
            CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream(content, writable: false);
            await fileStorage.UploadAsync(
                objectKey,
                stream,
                DealDocumentBuckets.DealDocuments,
                contentType,
                cancellationToken);
        }
    }
}
