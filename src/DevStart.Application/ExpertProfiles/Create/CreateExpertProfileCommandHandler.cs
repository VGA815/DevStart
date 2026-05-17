using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Experts;
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

            ExpertProfile expertProfile = ExpertProfile.Create(
                userId,
                command.DisplayName,
                command.Bio,
                command.Website,
                command.IsPublic,
                command.LinkedInUrl,
                command.TwitterUrl,
                command.GitHubUrl,
                command.TelegramUrl,
                dateTimeProvider.UtcNow);

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
