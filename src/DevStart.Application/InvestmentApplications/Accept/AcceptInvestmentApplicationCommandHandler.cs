using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentApplications.Accept
{
    internal sealed class AcceptInvestmentApplicationCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<AcceptInvestmentApplicationCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(AcceptInvestmentApplicationCommand command, CancellationToken cancellationToken)
        {
            InvestmentApplication? application = await context.InvestmentApplications
                .SingleOrDefaultAsync(a => a.Id == command.ApplicationId, cancellationToken);

            if (application is null)
            {
                return Result.Failure<Guid>(InvestmentApplicationErrors.NotFound(command.ApplicationId));
            }

            StartupMember? member = await context.StartupMembers
                .SingleOrDefaultAsync(
                    sm => sm.StartupId == application.StartupId && sm.ProfileId == userContext.UserId,
                    cancellationToken);

            if (member is null || member.Role == StartupRole.Member)
            {
                return Result.Failure<Guid>(InvestmentApplicationErrors.Unauthorized);
            }

            DateTime utcNow = dateTimeProvider.UtcNow;
            Result acceptResult = application.Accept(utcNow);

            if (acceptResult.IsFailure)
            {
                return Result.Failure<Guid>(acceptResult.Error);
            }

            InvestmentDeal deal = InvestmentDeal.CreateFromApplication(application, utcNow);
            context.InvestmentDeals.Add(deal);

            application.Raise(new InvestmentApplicationAcceptedDomainEvent(
                application.Id,
                deal.Id,
                application.InvestorProfileId,
                application.StartupId));

            await context.SaveChangesAsync(cancellationToken);

            return deal.Id;
        }
    }
}
