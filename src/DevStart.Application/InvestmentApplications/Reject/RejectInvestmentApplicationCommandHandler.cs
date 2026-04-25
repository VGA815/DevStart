using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentApplications.Reject
{
    internal sealed class RejectInvestmentApplicationCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<RejectInvestmentApplicationCommand>
    {
        public async Task<Result> Handle(RejectInvestmentApplicationCommand command, CancellationToken cancellationToken)
        {
            InvestmentApplication? application = await context.InvestmentApplications
                .SingleOrDefaultAsync(a => a.Id == command.ApplicationId, cancellationToken);

            if (application is null)
            {
                return Result.Failure(InvestmentApplicationErrors.NotFound(command.ApplicationId));
            }

            StartupMember? member = await context.StartupMembers
                .SingleOrDefaultAsync(
                    sm => sm.StartupId == application.StartupId && sm.ProfileId == userContext.UserId,
                    cancellationToken);

            if (member is null || member.Role == StartupRole.Member)
            {
                return Result.Failure(InvestmentApplicationErrors.Unauthorized);
            }

            Result rejectResult = application.Reject(dateTimeProvider.UtcNow);

            if (rejectResult.IsFailure)
            {
                return rejectResult;
            }

            application.Raise(new InvestmentApplicationRejectedDomainEvent(
                application.Id,
                application.InvestorProfileId,
                application.StartupId));

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
