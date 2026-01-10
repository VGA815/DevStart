using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupInvestors;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupInvestors.ChangeVisibility
{
    internal sealed class ChangeStartupInvestorVisibilityCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<ChangeStartupInvestorVisibilityCommand>
    {
        public async Task<Result> Handle(ChangeStartupInvestorVisibilityCommand command, CancellationToken cancellationToken)
        {
            StartupInvestor? startupInvestor = await context.StartupInvestors
                .SingleOrDefaultAsync(si => si.StartupId == command.StartupId && si.ProfileId == userContext.UserId, cancellationToken);

            if (startupInvestor == null)
            {
                return Result.Failure(StartupInvestorErrors.NotFound(userContext.UserId, command.StartupId));
            }

            startupInvestor.IsPublic = command.IsPublic;
            startupInvestor.UpdatedAt = dateTimeProvider.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
