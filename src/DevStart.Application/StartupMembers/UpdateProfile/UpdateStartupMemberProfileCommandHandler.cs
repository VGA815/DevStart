using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMembers.UpdateProfile
{
    internal sealed class UpdateStartupMemberProfileCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService)
        : ICommandHandler<UpdateStartupMemberProfileCommand>
    {
        public async Task<Result> Handle(UpdateStartupMemberProfileCommand command, CancellationToken cancellationToken)
        {
            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(
                    sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId,
                    cancellationToken);

            if (startupMember is null)
            {
                return Result.Failure(StartupMemberErrors.NotFound(userContext.UserId, command.StartupId));
            }

            Result updateResult = startupMember.UpdateProfile(
                command.Position,
                command.YearsOfExperience,
                command.HasPriorExit,
                command.PreviousStartupsCount,
                dateTimeProvider.UtcNow);

            if (updateResult.IsFailure)
            {
                return updateResult;
            }

            await context.SaveChangesAsync(cancellationToken);

            await cacheService.RemoveAsync(CacheKeys.StartupScore(command.StartupId), cancellationToken);

            return Result.Success();
        }
    }
}
