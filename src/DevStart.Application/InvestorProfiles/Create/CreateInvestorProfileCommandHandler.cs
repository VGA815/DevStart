using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Investors;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestorProfiles.Create
{
    internal sealed class CreateInvestorProfileCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateInvestorProfileCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateInvestorProfileCommand command, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            bool alreadyExists = await context.InvestorProfiles
                .AnyAsync(ip => ip.UserId == userId, cancellationToken);

            if (alreadyExists)
            {
                return Result.Failure<Guid>(InvestorProfileErrors.AlreadyExists(userId));
            }

            InvestorProfile investorProfile = InvestorProfile.Create(
                userId,
                command.Type,
                command.DisplayName,
                command.Bio,
                command.Website,
                command.IsPublic,
                dateTimeProvider.UtcNow);

            context.InvestorProfiles.Add(investorProfile);
            await context.SaveChangesAsync(cancellationToken);

            return investorProfile.Id;
        }
    }
}
