using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupCompetitors;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupCompetitors.Create
{
    internal sealed class CreateStartupCompetitorCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateStartupCompetitorCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateStartupCompetitorCommand command, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(s => s.Id == command.StartupId, cancellationToken))
            {
                return Result.Failure<Guid>(StartupErrors.NotFound(command.StartupId));
            }

            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(
                    sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId,
                    cancellationToken);

            if (startupMember is null)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            if (startupMember.Role == StartupRole.Member)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            StartupCompetitor competitor = StartupCompetitor.Create(
                command.StartupId,
                command.Name,
                command.Website,
                command.Description,
                command.StrengthsVsUs,
                command.WeaknessesVsUs,
                dateTimeProvider.UtcNow);

            context.StartupCompetitors.Add(competitor);

            await context.SaveChangesAsync(cancellationToken);

            return competitor.Id;
        }
    }
}
