using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentApplications;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentApplications.Withdraw
{
    internal sealed class WithdrawInvestmentApplicationCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<WithdrawInvestmentApplicationCommand>
    {
        public async Task<Result> Handle(WithdrawInvestmentApplicationCommand command, CancellationToken cancellationToken)
        {
            InvestmentApplication? application = await context.InvestmentApplications
                .SingleOrDefaultAsync(a => a.Id == command.ApplicationId, cancellationToken);

            if (application is null)
            {
                return Result.Failure(InvestmentApplicationErrors.NotFound(command.ApplicationId));
            }

            if (application.InvestorProfileId != userContext.UserId)
            {
                return Result.Failure(InvestmentApplicationErrors.Unauthorized);
            }

            Result withdrawResult = application.Withdraw(dateTimeProvider.UtcNow);

            if (withdrawResult.IsFailure)
            {
                return withdrawResult;
            }

            application.Raise(new InvestmentApplicationWithdrawnDomainEvent(
                application.Id,
                application.InvestorProfileId,
                application.StartupId));

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
