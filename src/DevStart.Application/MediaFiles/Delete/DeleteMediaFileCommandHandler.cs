using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.MediaFiles;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.MediaFiles.Delete
{
    internal sealed class DeleteMediaFileCommandHandler(IApplicationDbContext context, IUserContext userContext, IFileStorage fileStorage)
        : ICommandHandler<DeleteMediaFileCommand>
    {
        public async Task<Result> Handle(DeleteMediaFileCommand command, CancellationToken cancellationToken)
        {
            MediaFile? mediaFile = await context.MediaFiles.SingleOrDefaultAsync(mf => mf.Id == command.FileId, cancellationToken);

            if (mediaFile == null)
            {
                return Result.Failure(MediaFileErrors.NotFound(command.FileId));
            }

            if (mediaFile.UploaderId != userContext.UserId)
            {
                return Result.Failure(UserErrors.Unauthorized());
            }

            try
            {
                await fileStorage.DeleteAsync(
                    mediaFile.ObjectName,
                    mediaFile.Bucket,
                    cancellationToken);
            }
            catch (FileStorageException ex) when (!ex.NotFound)
            {
                // The object could not be removed (storage outage). Keep the DB record so the delete can
                // be retried — don't orphan it. A missing object (NotFound) falls through as success.
                return Result.Failure(MediaFileErrors.StorageUnavailable);
            }

            context.MediaFiles.Remove(mediaFile);
            await context.SaveChangesAsync(cancellationToken);
            
            return Result.Success();
        }
    }
}
