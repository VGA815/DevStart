using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Experts;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertExperiences.Delete
{
    internal sealed class DeleteExpertExperienceCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : ICommandHandler<DeleteExpertExperienceCommand>
    {
        public async Task<Result> Handle(DeleteExpertExperienceCommand command, CancellationToken cancellationToken)
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

            context.ExpertExperiences.Remove(experience);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
