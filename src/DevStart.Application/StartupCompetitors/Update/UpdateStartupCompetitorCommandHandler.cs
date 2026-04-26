using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupCompetitors;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupCompetitors.Update
{
    internal sealed class UpdateStartupCompetitorCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UpdateStartupCompetitorCommand>
    {
        public async Task<Result> Handle(UpdateStartupCompetitorCommand command, CancellationToken cancellationToken)
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

            competitor.Update(
                command.Name,
                command.Website,
                command.Description,
                command.StrengthsVsUs,
                command.WeaknessesVsUs,
                dateTimeProvider.UtcNow);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
