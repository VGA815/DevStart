using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Experts;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertExperiences.Update
{
    internal sealed class UpdateExpertExperienceCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UpdateExpertExperienceCommand>
    {
        public async Task<Result> Handle(UpdateExpertExperienceCommand command, CancellationToken cancellationToken)
        {
            ExpertExperience? experience = await context.ExpertExperiences
                .SingleOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

            if (experience is null)
            {
                return Result.Failure(ExpertExperienceErrors.NotFound(command.Id));
            }

            ExpertProfile? expertProfile = await context.ExpertProfiles
                .SingleOrDefaultAsync(ep => ep.Id == experience.ExpertProfileId, cancellationToken);

            if (expertProfile is null)
            {
                return Result.Failure(ExpertExperienceErrors.ExpertProfileNotFound);
            }

            if (expertProfile.UserId != userContext.UserId)
            {
                return Result.Failure(ExpertExperienceErrors.Unauthorized);
            }

            experience.Update(
                command.Company,
                command.Position,
                command.StartDate,
                command.EndDate,
                command.Description,
                dateTimeProvider.UtcNow);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
