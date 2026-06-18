using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Investors;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestorProfiles.Update
{
    internal sealed class UpdateInvestorProfileCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UpdateInvestorProfileCommand>
    {
        public async Task<Result> Handle(UpdateInvestorProfileCommand command, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            InvestorProfile? investorProfile = await context.InvestorProfiles
                .SingleOrDefaultAsync(ip => ip.UserId == userId, cancellationToken);

            if (investorProfile is null)
            {
                return Result.Failure(InvestorProfileErrors.NotFound(userId));
            }

            investorProfile.Update(
                command.Type,
                dateTimeProvider.UtcNow);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
