using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Profiles;
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

            // Tracked: the personal fields of the investor form are stored here, not on InvestorProfile.
            Profile? profile = await context.Profiles
                .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (profile is null)
            {
                return Result.Failure<Guid>(ProfileErrors.NotFound(userId));
            }

            if (string.IsNullOrWhiteSpace(command.DisplayName))
            {
                return Result.Failure<Guid>(InvestorProfileErrors.ProfileNameRequired);
            }

            // Core fields only: the investor form has no social-link inputs, and this user may also
            // hold an expert profile whose links must survive.
            ProfilePersonalDetails.ApplyCore(
                profile, command.DisplayName, command.Bio, command.Website, command.IsPublic);

            InvestorProfile investorProfile = InvestorProfile.Create(
                userId,
                command.Type,
                dateTimeProvider.UtcNow,
                command.AvatarId);

            context.InvestorProfiles.Add(investorProfile);
            await context.SaveChangesAsync(cancellationToken);

            return investorProfile.Id;
        }
    }
}
