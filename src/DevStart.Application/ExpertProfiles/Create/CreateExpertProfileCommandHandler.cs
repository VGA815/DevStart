using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Profiles;
using DevStart.Domain.Experts;
using DevStart.Domain.Profiles;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertProfiles.Create
{
    internal sealed class CreateExpertProfileCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateExpertProfileCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateExpertProfileCommand command, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            bool alreadyExists = await context.ExpertProfiles
                .AnyAsync(ep => ep.UserId == userId, cancellationToken);

            if (alreadyExists)
            {
                return Result.Failure<Guid>(ExpertProfileErrors.AlreadyExists(userId));
            }

            // Tracked: the personal fields of the expert form are stored here, not on ExpertProfile.
            Profile? profile = await context.Profiles
                .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (profile is null)
            {
                return Result.Failure<Guid>(ProfileErrors.NotFound(userId));
            }

            if (string.IsNullOrWhiteSpace(command.DisplayName))
            {
                return Result.Failure<Guid>(ExpertProfileErrors.ProfileNameRequired);
            }

            ProfilePersonalDetails.ApplyCore(
                profile, command.DisplayName, command.Bio, command.Website, command.IsPublic);
            ProfilePersonalDetails.ApplySocialLinks(
                profile, command.LinkedInUrl, command.TwitterUrl, command.GitHubUrl, command.TelegramUrl);

            ExpertProfile expertProfile = ExpertProfile.Create(userId, dateTimeProvider.UtcNow);

            context.ExpertProfiles.Add(expertProfile);

            foreach (ExpertSpecialization specialization in command.Specializations.Distinct())
            {
                context.ExpertProfileSpecializations.Add(
                    ExpertProfileSpecialization.Create(expertProfile.Id, specialization));
            }

            await context.SaveChangesAsync(cancellationToken);

            return expertProfile.Id;
        }
    }
}
