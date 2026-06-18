using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
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

            // Personal data (name, bio, links) lives on the shared Profile; require it before becoming an expert.
            Profile? profile = await context.Profiles
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (profile is null || string.IsNullOrWhiteSpace(profile.Name))
            {
                return Result.Failure<Guid>(ExpertProfileErrors.ProfileNameRequired);
            }

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
