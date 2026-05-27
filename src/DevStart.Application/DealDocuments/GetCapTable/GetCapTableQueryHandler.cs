using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Subscriptions;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Domain.DealDocuments;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DevStart.Application.DealDocuments.GetCapTable
{
    internal sealed class GetCapTableQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IFileStorage fileStorage,
        ISubscriptionChecker subscriptionChecker)
        : IQueryHandler<GetCapTableQuery, CapTableResult>
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task<Result<CapTableResult>> Handle(GetCapTableQuery query, CancellationToken cancellationToken)
        {
            InvestmentDeal? deal = await context.InvestmentDeals
                .AsNoTracking()
                .SingleOrDefaultAsync(d => d.Id == query.DealId, cancellationToken);
            if (deal is null)
            {
                return Result.Failure<CapTableResult>(InvestmentDealErrors.NotFound(query.DealId));
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
                return Result.Failure<CapTableResult>(DealDocumentErrors.Unauthorized);
            }

            if (isInvestor && !await subscriptionChecker.HasActiveProAsync(userId, cancellationToken))
            {
                return Result.Failure<CapTableResult>(SubscriptionErrors.ProRequired);
            }

            DealDocument? doc = await context.DealDocuments
                .AsNoTracking()
                .SingleOrDefaultAsync(d => d.DealId == query.DealId, cancellationToken);
            if (doc is null)
            {
                return Result.Failure<CapTableResult>(DealDocumentErrors.NotFound(query.DealId));
            }

            CapTableResult? capTable;
            try
            {
                using Stream stream = await fileStorage.DownloadAsync(
                    doc.CapTableObjectKey,
                    DealDocumentBuckets.DealDocuments,
                    cancellationToken);

                capTable = await JsonSerializer.DeserializeAsync<CapTableResult>(
                    stream, SerializerOptions, cancellationToken);
            }
            catch (FileStorageException ex)
            {
                return Result.Failure<CapTableResult>(
                    ex.NotFound ? DealDocumentErrors.NotFound(query.DealId) : DealDocumentErrors.StorageUnavailable);
            }
            catch (JsonException)
            {
                return Result.Failure<CapTableResult>(DealDocumentErrors.NotFound(query.DealId));
            }

            if (capTable is null)
            {
                return Result.Failure<CapTableResult>(DealDocumentErrors.NotFound(query.DealId));
            }

            return capTable;
        }
    }
}
