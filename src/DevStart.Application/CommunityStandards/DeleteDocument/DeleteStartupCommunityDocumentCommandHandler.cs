using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.CommunityStandards.DeleteDocument
{
    internal sealed class DeleteStartupCommunityDocumentCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IStartupAuthorizationService authorizationService,
        ICommunityStandardsRefresher refresher)
        : ICommandHandler<DeleteStartupCommunityDocumentCommand>
    {
        public async Task<Result> Handle(
            DeleteStartupCommunityDocumentCommand command,
            CancellationToken cancellationToken)
        {
            StartupCommunityDocument? document = await context.StartupCommunityDocuments
                .SingleOrDefaultAsync(
                    d => d.StartupId == command.StartupId && d.Type == command.Type, cancellationToken);

            if (document is null)
            {
                return Result.Failure(
                    StartupCommunityDocumentErrors.NotFound(command.StartupId, command.Type));
            }

            if (!await authorizationService.IsFounderOrAdminAsync(
                    userContext.UserId, command.StartupId, cancellationToken))
            {
                return Result.Failure(StartupCommunityDocumentErrors.Unauthorized);
            }

            context.StartupCommunityDocuments.Remove(document);
            await context.SaveChangesAsync(cancellationToken);

            await refresher.RefreshAsync(command.StartupId, cancellationToken);

            return Result.Success();
        }
    }
}
