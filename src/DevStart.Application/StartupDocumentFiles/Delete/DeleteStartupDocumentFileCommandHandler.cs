using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupDocumentFiles.Delete
{
    internal sealed class DeleteStartupDocumentFileCommandHandler(IApplicationDbContext context, IUserContext userContext, IFileStorage fileStorage)
        : ICommandHandler<DeleteStartupDocumentFileCommand>
    {
        public async Task<Result> Handle(DeleteStartupDocumentFileCommand command, CancellationToken cancellationToken)
        {
            StartupDocumentFile? startupDocumentFile = await context.StartupDocumentFiles.SingleOrDefaultAsync(sdf => sdf.Id == command.StartupDocumentFileId, cancellationToken);

            if (startupDocumentFile == null)
            {
                return Result.Failure(StartupDocumentFileErrors.NotFound(command.StartupDocumentFileId));
            }

            StartupMember? startupMember = await context.StartupMembers.SingleOrDefaultAsync(sm => sm.ProfileId == userContext.UserId && sm.StartupId == startupDocumentFile.StartupId, cancellationToken);

            if (startupMember == null)
            {
                return Result.Failure(UserErrors.Unauthorized());
            }

            try
            {
                await fileStorage.DeleteAsync(
                    startupDocumentFile.ObjectName,
                    startupDocumentFile.Bucket,
                    cancellationToken);
            }
            catch (FileStorageException ex) when (!ex.NotFound)
            {
                // Keep the DB record so the delete can be retried; a missing object falls through.
                return Result.Failure(StartupDocumentFileErrors.StorageUnavailable);
            }

            context.StartupDocumentFiles.Remove(startupDocumentFile);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
