using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupRoadmapItems;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupRoadmapItems.Delete
{
    internal sealed class DeleteStartupRoadmapItemCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<DeleteStartupRoadmapItemCommand>
    {
        public async Task<Result> Handle(DeleteStartupRoadmapItemCommand command, CancellationToken cancellationToken)
        {
            StartupRoadmapItem? startupRoadmapItem = await context.StartupRoadmapItems
                .SingleOrDefaultAsync(sri => sri.Id == command.ItemId, cancellationToken);

            if (startupRoadmapItem == null)
            {
                return Result.Failure(StartupRoadmapItemErrors.NotFound(command.ItemId));
            }

            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == startupRoadmapItem.StartupId && sm.ProfileId == userContext.UserId, cancellationToken);

            if (startupMember == null)
            {
                return Result.Failure(UserErrors.Unauthorized());
            }

            context.StartupRoadmapItems.Remove(startupRoadmapItem);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
