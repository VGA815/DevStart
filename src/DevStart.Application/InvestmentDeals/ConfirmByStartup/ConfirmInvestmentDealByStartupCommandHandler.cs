using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentDeals.ConfirmByStartup
{
    internal sealed class ConfirmInvestmentDealByStartupCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<ConfirmInvestmentDealByStartupCommand>
    {
        public async Task<Result> Handle(ConfirmInvestmentDealByStartupCommand command, CancellationToken cancellationToken)
        {
            InvestmentDeal? deal = await context.InvestmentDeals
                .SingleOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);

            if (deal is null)
            {
                return Result.Failure(InvestmentDealErrors.NotFound(command.DealId));
            }

            StartupMember? member = await context.StartupMembers
                .SingleOrDefaultAsync(
                    sm => sm.StartupId == deal.StartupId && sm.ProfileId == userContext.UserId,
                    cancellationToken);

            if (member is null || member.Role == StartupRole.Member)
            {
                return Result.Failure(InvestmentDealErrors.Unauthorized);
            }

            Result confirmResult = deal.ConfirmByStartup(dateTimeProvider.UtcNow);

            if (confirmResult.IsFailure)
            {
                return confirmResult;
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
