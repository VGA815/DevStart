using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupCompetitors;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupCompetitors.Delete
{
    internal sealed class DeleteStartupCompetitorCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : ICommandHandler<DeleteStartupCompetitorCommand>
    {
        public async Task<Result> Handle(DeleteStartupCompetitorCommand command, CancellationToken cancellationToken)
        {
            StartupCompetitor? competitor = await context.StartupCompetitors
                .SingleOrDefaultAsync(c => c.Id == command.CompetitorId, cancellationToken);

            if (competitor is null)
            {
                return Result.Failure(StartupCompetitorErrors.NotFound(command.CompetitorId));
            }

            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(
                    sm => sm.StartupId == competitor.StartupId && sm.ProfileId == userContext.UserId,
                    cancellationToken);

            if (startupMember is null || startupMember.Role == StartupRole.Member)
            {
                return Result.Failure(StartupCompetitorErrors.Unauthorized);
            }

            context.StartupCompetitors.Remove(competitor);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
