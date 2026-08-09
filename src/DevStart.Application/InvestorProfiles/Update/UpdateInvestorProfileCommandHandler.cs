using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Profiles;
using DevStart.Domain.Investors;
using DevStart.Domain.Profiles;
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

            // The personal fields of the investor form are stored on the shared Profile.
            Profile? profile = await context.Profiles
                .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (profile is null)
            {
                return Result.Failure(ProfileErrors.NotFound(userId));
            }

            if (string.IsNullOrWhiteSpace(command.DisplayName))
            {
                return Result.Failure(InvestorProfileErrors.ProfileNameRequired);
            }

            // Core fields only: the investor form has no social-link inputs, and this user may also
            // hold an expert profile whose links must survive.
            ProfilePersonalDetails.ApplyCore(
                profile, command.DisplayName, command.Bio, command.Website, command.IsPublic);

            investorProfile.Update(
                command.Type,
                dateTimeProvider.UtcNow);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
