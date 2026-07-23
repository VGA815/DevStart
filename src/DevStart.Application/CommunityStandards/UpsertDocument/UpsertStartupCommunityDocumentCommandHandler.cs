using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.CommunityStandards.UpsertDocument
{
    internal sealed class UpsertStartupCommunityDocumentCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IStartupAuthorizationService authorizationService,
        ICommunityStandardsRefresher refresher,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UpsertStartupCommunityDocumentCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(
            UpsertStartupCommunityDocumentCommand command,
            CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(s => s.Id == command.StartupId, cancellationToken))
            {
                return Result.Failure<Guid>(StartupErrors.NotFound(command.StartupId));
            }

            Guid userId = userContext.UserId;
            if (!await authorizationService.IsFounderOrAdminAsync(userId, command.StartupId, cancellationToken))
            {
                return Result.Failure<Guid>(StartupCommunityDocumentErrors.Unauthorized);
            }

            DateTime utcNow = dateTimeProvider.UtcNow;

            StartupCommunityDocument? document = await context.StartupCommunityDocuments
                .SingleOrDefaultAsync(
                    d => d.StartupId == command.StartupId && d.Type == command.Type, cancellationToken);

            if (document is null)
            {
                document = StartupCommunityDocument.Create(
                    command.StartupId, command.Type, command.Title, command.Content, userId, utcNow);

                context.StartupCommunityDocuments.Add(document);
            }
            else
            {
                document.Update(command.Title, command.Content, userId, utcNow);
            }

            await context.SaveChangesAsync(cancellationToken);

            // Publishing a document flips a checklist row, so the projection and the cached checklist
            // have to catch up immediately rather than wait for the nightly sweep.
            await refresher.RefreshAsync(command.StartupId, cancellationToken);

            return document.Id;
        }
    }
}
