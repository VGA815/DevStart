using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.ServiceOrders;
using DevStart.Application.Abstractions.Subscriptions;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Domain.DealDocuments;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.DealDocuments.GetTermSheetDownloadUrl
{
    internal sealed class GetTermSheetDownloadUrlQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IFileStorage fileStorage,
        IDateTimeProvider dateTimeProvider,
        ISubscriptionChecker subscriptionChecker,
        IServiceEntitlementChecker entitlementChecker)
        : IQueryHandler<GetTermSheetDownloadUrlQuery, TermSheetDownloadUrlResponse>
    {
        private const int ExpirySeconds = 600; // 10 min

        public async Task<Result<TermSheetDownloadUrlResponse>> Handle(
            GetTermSheetDownloadUrlQuery query,
            CancellationToken cancellationToken)
        {
            InvestmentDeal? deal = await context.InvestmentDeals
                .AsNoTracking()
                .SingleOrDefaultAsync(d => d.Id == query.DealId, cancellationToken);
            if (deal is null)
            {
                return Result.Failure<TermSheetDownloadUrlResponse>(InvestmentDealErrors.NotFound(query.DealId));
            }

            Guid userId = userContext.UserId;
            bool isInvestor = deal.InvestorProfileId == userId;
            bool isFounderOrAdmin = !isInvestor && await context.StartupMembers
                .AsNoTracking()
                .AnyAsync(sm => sm.StartupId == deal.StartupId
                             && sm.ProfileId == userId
                             && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration),
                          cancellationToken);
            if (!isInvestor && !isFounderOrAdmin)
            {
                return Result.Failure<TermSheetDownloadUrlResponse>(DealDocumentErrors.Unauthorized);
            }

            // Investor side requires Pro, or a paid one-time term-sheet order for this deal (SC-49).
            if (isInvestor
                && !await subscriptionChecker.HasActiveProAsync(userId, cancellationToken)
                && !await entitlementChecker.HasAsync(
                        userId, ServiceType.TermSheet, query.DealId, cancellationToken))
            {
                return Result.Failure<TermSheetDownloadUrlResponse>(SubscriptionErrors.ProRequired);
            }

            DealDocument? doc = await context.DealDocuments
                .AsNoTracking()
                .SingleOrDefaultAsync(d => d.DealId == query.DealId, cancellationToken);
            if (doc is null)
            {
                return Result.Failure<TermSheetDownloadUrlResponse>(DealDocumentErrors.NotFound(query.DealId));
            }

            bool wantsPdf = query.Format == TermSheetFormat.Pdf;

            // A document set generated before PDF rendering existed carries an empty PDF key. The
            // generation job fills those in when it next runs for the deal; until then the honest
            // answer is that this rendering does not exist, not a link to nothing.
            if (wantsPdf && !doc.HasPdf)
            {
                return Result.Failure<TermSheetDownloadUrlResponse>(
                    DealDocumentErrors.PdfNotGenerated(query.DealId));
            }

            string objectKey = wantsPdf ? doc.TermSheetPdfObjectKey : doc.TermSheetObjectKey;
            string fileName = FileName(query.DealId, doc.GeneratedAt, wantsPdf);

            string url;
            try
            {
                url = await fileStorage.GetPresignedUrl(
                    objectKey,
                    DealDocumentBuckets.DealDocuments,
                    ExpirySeconds,
                    cancellationToken,
                    fileName);
            }
            catch (FileStorageException ex)
            {
                return Result.Failure<TermSheetDownloadUrlResponse>(
                    ex.NotFound ? DealDocumentErrors.NotFound(query.DealId) : DealDocumentErrors.StorageUnavailable);
            }

            return new TermSheetDownloadUrlResponse
            {
                DealId = query.DealId,
                Url = url,
                ExpiresAt = dateTimeProvider.UtcNow.AddSeconds(ExpirySeconds),
                Format = query.Format,
                FileName = fileName,
                Sha256 = wantsPdf ? doc.TermSheetPdfSha256 : null
            };
        }

        /// <summary>
        /// Deal and date, so that several downloaded term sheets can be told apart in a downloads
        /// folder. ASCII only and no startup name: the value is signed into a Content-Disposition
        /// header, where a Cyrillic name is an encoding problem rather than a nicety.
        /// </summary>
        private static string FileName(Guid dealId, DateTime generatedAt, bool pdf) =>
            $"term-sheet-{dealId}-{generatedAt:yyyy-MM-dd}.{(pdf ? "pdf" : "md")}";
    }
}
