using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Investors;
using DevStart.Domain.Profiles;
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

            // Personal data (name, bio, website) lives on the shared Profile; require it before becoming an investor.
            Profile? profile = await context.Profiles
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (profile is null || string.IsNullOrWhiteSpace(profile.Name))
            {
                return Result.Failure<Guid>(InvestorProfileErrors.ProfileNameRequired);
            }

            InvestorProfile investorProfile = InvestorProfile.Create(
                userId,
                command.Type,
                dateTimeProvider.UtcNow);

            context.InvestorProfiles.Add(investorProfile);
            await context.SaveChangesAsync(cancellationToken);

            return investorProfile.Id;
        }
    }
}
