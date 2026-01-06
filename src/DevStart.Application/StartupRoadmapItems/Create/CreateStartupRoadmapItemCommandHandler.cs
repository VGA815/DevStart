using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupRoadmapItems;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupRoadmapItems.Create
{
    internal sealed class CreateStartupRoadmapItemCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateStartupRoadmapItemCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateStartupRoadmapItemCommand command, CancellationToken cancellationToken)
        {
            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId, cancellationToken);

            if (startupMember == null)
            {
                return Result.Failure<Guid>(UserErrors.NotFound(userContext.UserId));
            }
            if (startupMember.Role == StartupRole.Member)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            StartupRoadmapItem startupRoadmapItem = new StartupRoadmapItem(
                command.StartupId,
                command.StartupStage,
                command.Title,
                command.Desription,
                command.Status,
                dateTimeProvider,
                command.TargetDate);

            context.StartupRoadmapItems.Add(startupRoadmapItem);

            await context.SaveChangesAsync(cancellationToken);

            return startupRoadmapItem.Id;
        }
    }
}
