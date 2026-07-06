using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupEquity.Vesting;
using DevStart.Application.Startups;
using DevStart.Domain.StartupEquity;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupEquity.GetCapTable
{
    internal sealed class GetStartupCapTableQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IStartupAuthorizationService authorizationService,
        IFoundingCapTableProvider capTableProvider,
        IVestingCalculator vestingCalculator,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetStartupCapTableQuery, StartupCapTableResponse>
    {
        public async Task<Result<StartupCapTableResponse>> Handle(
            GetStartupCapTableQuery query,
            CancellationToken cancellationToken)
        {
            if (!await authorizationService.IsFounderOrAdminAsync(userContext.UserId, query.StartupId, cancellationToken))
            {
                return Result.Failure<StartupCapTableResponse>(StartupEquityErrors.Unauthorized);
            }

            bool isConfigured = await context.StartupEquityHolders
                .AsNoTracking()
                .AnyAsync(h => h.StartupId == query.StartupId, cancellationToken);

            IReadOnlyList<FoundingCapTableHolder> holders =
                await capTableProvider.GetEffectiveHoldersAsync(query.StartupId, cancellationToken);

            DateTime asOf = dateTimeProvider.UtcNow;

            var holderResponses = new List<StartupCapTableHolderResponse>(holders.Count);
            foreach (FoundingCapTableHolder h in holders)
            {
                decimal vestedFraction = vestingCalculator.VestedFraction(
                    h.VestingStartDate, h.VestingMonths, h.CliffMonths, asOf);
                decimal vestedPct = Math.Round(h.EquityPercentage * vestedFraction, 2, MidpointRounding.AwayFromZero);

                holderResponses.Add(new StartupCapTableHolderResponse
                {
                    ProfileId = h.ProfileId,
                    HolderType = h.HolderType,
                    Name = h.Name,
                    EquityPercentage = h.EquityPercentage,
                    VestingStartDate = h.VestingStartDate,
                    VestingMonths = h.VestingMonths,
                    CliffMonths = h.CliffMonths,
                    VestedFraction = Math.Round(vestedFraction, 4, MidpointRounding.AwayFromZero),
                    VestedPercentage = vestedPct
                });
            }

            var response = new StartupCapTableResponse
            {
                StartupId = query.StartupId,
                IsConfigured = isConfigured,
                TotalPercentage = Math.Round(holderResponses.Sum(h => h.EquityPercentage), 2, MidpointRounding.AwayFromZero),
                TotalVestedPercentage = Math.Round(holderResponses.Sum(h => h.VestedPercentage), 2, MidpointRounding.AwayFromZero),
                Holders = holderResponses
            };

            return response;
        }
    }
}
