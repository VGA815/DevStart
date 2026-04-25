using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentDeals;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentDeals.ConfirmByInvestor
{
    internal sealed class ConfirmInvestmentDealByInvestorCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<ConfirmInvestmentDealByInvestorCommand>
    {
        public async Task<Result> Handle(ConfirmInvestmentDealByInvestorCommand command, CancellationToken cancellationToken)
        {
            InvestmentDeal? deal = await context.InvestmentDeals
                .SingleOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);

            if (deal is null)
            {
                return Result.Failure(InvestmentDealErrors.NotFound(command.DealId));
            }

            if (deal.InvestorProfileId != userContext.UserId)
            {
                return Result.Failure(InvestmentDealErrors.Unauthorized);
            }

            Result confirmResult = deal.ConfirmByInvestor(dateTimeProvider.UtcNow);

            if (confirmResult.IsFailure)
            {
                return confirmResult;
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
