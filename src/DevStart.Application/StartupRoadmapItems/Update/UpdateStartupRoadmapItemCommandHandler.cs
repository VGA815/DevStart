using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupRoadmapItems;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupRoadmapItems.Update
{
    internal sealed class UpdateStartupRoadmapItemCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<UpdateStartupRoadmapItemCommand>
    {
        public async Task<Result> Handle(UpdateStartupRoadmapItemCommand command, CancellationToken cancellationToken)
        {
            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId, cancellationToken);

            if (startupMember == null)
            {
                return Result.Failure(StartupMemberErrors.NotFound(userContext.UserId, command.StartupId));
            }
            
            StartupRoadmapItem? startupRoadmapItem = await context.StartupRoadmapItems
                .SingleOrDefaultAsync(sri => sri.Id == command.ItemId, cancellationToken);

            if (startupRoadmapItem == null)
            {
                return Result.Failure(StartupRoadmapItemErrors.NotFound(command.ItemId));
            }

            startupRoadmapItem.Desription = command.Desription;
            startupRoadmapItem.Status = command.Status;
            startupRoadmapItem.StartupStage = command.StartupStage;
            startupRoadmapItem.TargetDate = command.TargetDate;
            
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
