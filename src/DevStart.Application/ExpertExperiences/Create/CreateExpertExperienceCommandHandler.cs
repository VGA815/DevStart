using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Experts;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertExperiences.Create
{
    internal sealed class CreateExpertExperienceCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateExpertExperienceCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateExpertExperienceCommand command, CancellationToken cancellationToken)
        {
            ExpertProfile? expertProfile = await context.ExpertProfiles
                .SingleOrDefaultAsync(ep => ep.Id == command.ExpertProfileId, cancellationToken);

            if (expertProfile is null)
            {
                return Result.Failure<Guid>(ExpertExperienceErrors.ExpertProfileNotFound);
            }

            if (expertProfile.UserId != userContext.UserId)
            {
                return Result.Failure<Guid>(ExpertExperienceErrors.Unauthorized);
            }

            ExpertExperience experience = ExpertExperience.Create(
                command.ExpertProfileId,
                command.Company,
                command.Position,
                command.StartDate,
                command.EndDate,
                command.Description,
                dateTimeProvider.UtcNow);

            context.ExpertExperiences.Add(experience);
            await context.SaveChangesAsync(cancellationToken);

            return experience.Id;
        }
    }
}
