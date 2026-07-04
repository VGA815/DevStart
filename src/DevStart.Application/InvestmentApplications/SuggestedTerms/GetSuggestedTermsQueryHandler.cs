using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Subscriptions;
using DevStart.Application.Abstractions.Validation;
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
        IApplicationDbContext context,
        IDealTermsValidator dealTermsValidator)
        : IQueryHandler<GetSuggestedTermsQuery, SuggestedTermsResponse>
    {
        // Spec defaults:
        // - suggested cap = valuation_high × 1.05 (Safe / ConvertibleLoan)
        // - suggested pre-money = valuation point estimate (PricedRound)
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
            if (score.MethodsUsed.Count == 0 || score.ValuationHigh <= 0m)
            {
                // No usable valuation (empty ensemble, or e.g. a pre-revenue startup whose target
                // round wipes out the VC pre-money) — never suggest a ₽0 cap as if it were real.
                return Result.Failure<SuggestedTermsResponse>(ValuationErrors.InsufficientData);
            }

            decimal suggestedCap = Math.Round(score.ValuationHigh * CapMultiplier, 0, MidpointRounding.AwayFromZero);

            decimal? cap = null;
            decimal? discount = null;
            decimal? interestRate = null;
            int? termMonths = null;
            decimal? preMoney = null;

            switch (query.Instrument)
            {
                case InvestmentInstrument.Safe:
                    cap = suggestedCap;
                    discount = DefaultDiscount;
                    break;
                case InvestmentInstrument.ConvertibleLoan:
                    cap = suggestedCap;
                    discount = DefaultDiscount;
                    interestRate = DefaultInterestRate;
                    termMonths = DefaultTermMonths;
                    break;
                case InvestmentInstrument.PricedRound:
                    // The +5% premium is cap logic (a ceiling above fair value); the suggested
                    // pre-money for a priced round is the ensemble point estimate itself.
                    preMoney = score.ValuationPoint;
                    break;
            }

            // The intended amount turns the suggestion into a concrete deal preview: the implied
            // investor share and the standard deal-terms warnings (dilution, amount vs cap, …).
            decimal? impliedShareFraction = query.Amount > 0m
                ? InvestorShareMath.ComputeShareFraction(
                    query.Instrument, query.Amount, cap, interestRate, termMonths, preMoney)
                : null;

            IReadOnlyList<DealTermsFlag> warnings = query.Amount > 0m
                ? dealTermsValidator.Validate(new DealTermsInput(
                    query.Instrument, query.Amount, cap, discount, interestRate, termMonths,
                    preMoney, DefaultLiquidationPreference, ProRataRights: false))
                : [];

            return new SuggestedTermsResponse
            {
                StartupId = query.StartupId,
                Instrument = query.Instrument,
                SuggestedValuationCap = cap,
                SuggestedDiscount = discount,
                SuggestedInterestRate = interestRate,
                SuggestedTermMonths = termMonths,
                SuggestedPreMoneyValuation = preMoney,
                SuggestedLiquidationPreference = DefaultLiquidationPreference,
                ImpliedInvestorSharePct = impliedShareFraction is { } share
                    ? Math.Round(share * 100m, 2, MidpointRounding.AwayFromZero)
                    : null,
                Warnings = warnings,
                ScoreReference = score.TotalScore,
                ValuationLowReference = score.ValuationLow,
                ValuationHighReference = score.ValuationHigh
            };
        }
    }
}
