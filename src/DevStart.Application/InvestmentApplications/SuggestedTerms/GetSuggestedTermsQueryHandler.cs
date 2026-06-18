using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Subscriptions;
using DevStart.Application.Scoring;
using DevStart.Application.Startups.GetScore;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentApplications.SuggestedTerms
{
    internal sealed class GetSuggestedTermsQueryHandler(
        IQueryHandler<ComputeStartupScoreQuery, ScoreResult> scoreHandler,
        IUserContext userContext,
        ISubscriptionChecker subscriptionChecker,
        IApplicationDbContext context)
        : IQueryHandler<GetSuggestedTermsQuery, SuggestedTermsResponse>
    {
        // Spec defaults:
        // - suggested cap = valuation_high × 1.05
        // - suggested discount = 20%
        // - suggested interest = 6%
        // - suggested term = 18m
        private const decimal CapMultiplier = 1.05m;
        private const decimal DefaultDiscount = 0.20m;
        private const decimal DefaultInterestRate = 0.06m;
        private const int DefaultTermMonths = 18;
        private const decimal DefaultLiquidationPreference = 1.0m;

        public async Task<Result<SuggestedTermsResponse>> Handle(
            GetSuggestedTermsQuery query,
            CancellationToken cancellationToken)
        {
            Guid viewerId = userContext.UserId;
            bool isMember = await context.StartupMembers
                .AsNoTracking()
                .AnyAsync(sm => sm.StartupId == query.StartupId && sm.ProfileId == viewerId, cancellationToken);

            if (!isMember && !await subscriptionChecker.HasActiveProAsync(viewerId, cancellationToken))
            {
                return Result.Failure<SuggestedTermsResponse>(SubscriptionErrors.ProRequired);
            }

            // Viewer already gated above (member-or-Pro); call the ungated compute path directly.
            Result<ScoreResult> scoreResult = await scoreHandler.Handle(
                new ComputeStartupScoreQuery(query.StartupId),
                cancellationToken);

            if (scoreResult.IsFailure)
            {
                return Result.Failure<SuggestedTermsResponse>(scoreResult.Error);
            }

            ScoreResult score = scoreResult.Value;
            decimal suggestedCap = Math.Round(score.ValuationHigh * CapMultiplier, 0);

            SuggestedTermsResponse response = query.Instrument switch
            {
                InvestmentInstrument.Safe => new SuggestedTermsResponse
                {
                    StartupId = query.StartupId,
                    Instrument = InvestmentInstrument.Safe,
                    SuggestedValuationCap = suggestedCap,
                    SuggestedDiscount = DefaultDiscount,
                    SuggestedLiquidationPreference = DefaultLiquidationPreference,
                    ScoreReference = score.TotalScore,
                    ValuationLowReference = score.ValuationLow,
                    ValuationHighReference = score.ValuationHigh
                },
                InvestmentInstrument.ConvertibleLoan => new SuggestedTermsResponse
                {
                    StartupId = query.StartupId,
                    Instrument = InvestmentInstrument.ConvertibleLoan,
                    SuggestedValuationCap = suggestedCap,
                    SuggestedDiscount = DefaultDiscount,
                    SuggestedInterestRate = DefaultInterestRate,
                    SuggestedTermMonths = DefaultTermMonths,
                    SuggestedLiquidationPreference = DefaultLiquidationPreference,
                    ScoreReference = score.TotalScore,
                    ValuationLowReference = score.ValuationLow,
                    ValuationHighReference = score.ValuationHigh
                },
                InvestmentInstrument.PricedRound => new SuggestedTermsResponse
                {
                    StartupId = query.StartupId,
                    Instrument = InvestmentInstrument.PricedRound,
                    SuggestedPreMoneyValuation = suggestedCap,
                    SuggestedLiquidationPreference = DefaultLiquidationPreference,
                    ScoreReference = score.TotalScore,
                    ValuationLowReference = score.ValuationLow,
                    ValuationHighReference = score.ValuationHigh
                },
                _ => new SuggestedTermsResponse
                {
                    StartupId = query.StartupId,
                    Instrument = query.Instrument,
                    SuggestedLiquidationPreference = DefaultLiquidationPreference,
                    ScoreReference = score.TotalScore,
                    ValuationLowReference = score.ValuationLow,
                    ValuationHighReference = score.ValuationHigh
                }
            };

            return response;
        }
    }
}
