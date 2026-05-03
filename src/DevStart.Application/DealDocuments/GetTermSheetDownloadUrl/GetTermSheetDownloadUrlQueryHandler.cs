using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Domain.DealDocuments;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.DealDocuments.GetTermSheetDownloadUrl
{
    internal sealed class GetTermSheetDownloadUrlQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IFileStorage fileStorage,
        IDateTimeProvider dateTimeProvider)
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

            DealDocument? doc = await context.DealDocuments
                .AsNoTracking()
                .SingleOrDefaultAsync(d => d.DealId == query.DealId, cancellationToken);
            if (doc is null)
            {
                return Result.Failure<TermSheetDownloadUrlResponse>(DealDocumentErrors.NotFound(query.DealId));
            }

            string url = await fileStorage.GetPresignedUrl(
                doc.TermSheetObjectKey,
                DealDocumentBuckets.DealDocuments,
                ExpirySeconds,
                cancellationToken);

            return new TermSheetDownloadUrlResponse
            {
                DealId = query.DealId,
                Url = url,
                ExpiresAt = dateTimeProvider.UtcNow.AddSeconds(ExpirySeconds)
            };
        }
    }
}
