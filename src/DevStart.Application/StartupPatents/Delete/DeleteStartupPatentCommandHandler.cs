using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupPatents;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupPatents.Delete
{
    internal sealed class DeleteStartupPatentCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ICacheService cacheService)
        : ICommandHandler<DeleteStartupPatentCommand>
    {
        public async Task<Result> Handle(DeleteStartupPatentCommand command, CancellationToken cancellationToken)
        {
            StartupPatent? patent = await context.StartupPatents
                .SingleOrDefaultAsync(p => p.Id == command.PatentId, cancellationToken);

            if (patent is null)
            {
                return Result.Failure(StartupPatentErrors.NotFound(command.PatentId));
            }

            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(
                    sm => sm.StartupId == patent.StartupId && sm.ProfileId == userContext.UserId,
                    cancellationToken);

            if (startupMember is null || startupMember.Role == StartupRole.Member)
            {
                return Result.Failure(StartupPatentErrors.Unauthorized);
            }

            Guid startupId = patent.StartupId;

            context.StartupPatents.Remove(patent);

            await context.SaveChangesAsync(cancellationToken);

            await cacheService.RemoveAsync(CacheKeys.StartupPatents(startupId), cancellationToken);
            await cacheService.RemoveAsync(CacheKeys.StartupScore(startupId), cancellationToken);

            return Result.Success();
        }
    }
}
