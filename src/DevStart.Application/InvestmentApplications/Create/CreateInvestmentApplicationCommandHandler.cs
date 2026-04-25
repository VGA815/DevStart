using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentApplications.Create
{
    internal sealed class CreateInvestmentApplicationCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateInvestmentApplicationCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateInvestmentApplicationCommand command, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            if (command.Amount <= 0)
            {
                return Result.Failure<Guid>(InvestmentApplicationErrors.InvalidAmount);
            }

            bool hasInvestorProfile = await context.InvestorProfiles
                .AnyAsync(ip => ip.UserId == userId, cancellationToken);

            if (!hasInvestorProfile)
            {
                return Result.Failure<Guid>(InvestmentApplicationErrors.InvestorProfileRequired);
            }

            bool startupExists = await context.Startups
                .AnyAsync(s => s.Id == command.StartupId, cancellationToken);

            if (!startupExists)
            {
                return Result.Failure<Guid>(StartupErrors.NotFound(command.StartupId));
            }

            bool isMember = await context.StartupMembers
                .AnyAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == userId, cancellationToken);

            if (isMember)
            {
                return Result.Failure<Guid>(InvestmentApplicationErrors.CannotApplyToOwnStartup);
            }

            if (command.RoadmapItemId.HasValue)
            {
                bool roadmapItemValid = await context.StartupRoadmapItems
                    .AnyAsync(ri => ri.Id == command.RoadmapItemId.Value && ri.StartupId == command.StartupId, cancellationToken);

                if (!roadmapItemValid)
                {
                    return Result.Failure<Guid>(InvestmentApplicationErrors.RoadmapItemNotFound);
                }
            }

            InvestmentApplication application = InvestmentApplication.Create(
                userId,
                command.StartupId,
                command.RoadmapItemId,
                command.Amount,
                command.Message,
                dateTimeProvider.UtcNow);

            application.Raise(new InvestmentApplicationCreatedDomainEvent(
                application.Id,
                application.InvestorProfileId,
                application.StartupId,
                application.Amount));

            context.InvestmentApplications.Add(application);
            await context.SaveChangesAsync(cancellationToken);

            return application.Id;
        }
    }
}
