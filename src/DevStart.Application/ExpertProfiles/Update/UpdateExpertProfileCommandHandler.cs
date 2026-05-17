using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Experts;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertProfiles.Update
{
    internal sealed class UpdateExpertProfileCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UpdateExpertProfileCommand>
    {
        public async Task<Result> Handle(UpdateExpertProfileCommand command, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            ExpertProfile? expertProfile = await context.ExpertProfiles
                .SingleOrDefaultAsync(ep => ep.UserId == userId, cancellationToken);

            if (expertProfile is null)
            {
                return Result.Failure(ExpertProfileErrors.NotFound(userId));
            }

            expertProfile.Update(
                command.DisplayName,
                command.Bio,
                command.Website,
                command.IsPublic,
                command.LinkedInUrl,
                command.TwitterUrl,
                command.GitHubUrl,
                command.TelegramUrl,
                dateTimeProvider.UtcNow);

            List<ExpertProfileSpecialization> existingSpecializations = await context.ExpertProfileSpecializations
                .Where(s => s.ExpertProfileId == expertProfile.Id)
                .ToListAsync(cancellationToken);

            context.ExpertProfileSpecializations.RemoveRange(existingSpecializations);

            foreach (ExpertSpecialization specialization in command.Specializations.Distinct())
            {
                context.ExpertProfileSpecializations.Add(
                    ExpertProfileSpecialization.Create(expertProfile.Id, specialization));
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
