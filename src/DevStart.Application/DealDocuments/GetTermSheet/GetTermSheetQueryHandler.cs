using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Domain.DealDocuments;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DevStart.Application.DealDocuments.GetTermSheet
{
    internal sealed class GetTermSheetQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IFileStorage fileStorage)
        : IQueryHandler<GetTermSheetQuery, TermSheetResponse>
    {
        public async Task<Result<TermSheetResponse>> Handle(GetTermSheetQuery query, CancellationToken cancellationToken)
        {
            InvestmentDeal? deal = await context.InvestmentDeals
                .AsNoTracking()
                .SingleOrDefaultAsync(d => d.Id == query.DealId, cancellationToken);
            if (deal is null)
            {
                return Result.Failure<TermSheetResponse>(InvestmentDealErrors.NotFound(query.DealId));
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
                return Result.Failure<TermSheetResponse>(DealDocumentErrors.Unauthorized);
            }

            DealDocument? doc = await context.DealDocuments
                .AsNoTracking()
                .SingleOrDefaultAsync(d => d.DealId == query.DealId, cancellationToken);
            if (doc is null)
            {
                return Result.Failure<TermSheetResponse>(DealDocumentErrors.NotFound(query.DealId));
            }

            using Stream stream = await fileStorage.DownloadAsync(
                doc.TermSheetObjectKey,
                DealDocumentBuckets.DealDocuments,
                cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string markdown = await reader.ReadToEndAsync(cancellationToken);

            return new TermSheetResponse
            {
                DealId = query.DealId,
                Markdown = markdown,
                GeneratedAt = doc.GeneratedAt
            };
        }
    }
}
