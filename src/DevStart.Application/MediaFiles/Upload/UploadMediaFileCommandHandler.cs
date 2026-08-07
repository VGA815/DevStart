using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.MediaFiles;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.MediaFiles.Upload
{
    internal sealed class UploadMediaFileCommandHandler(IApplicationDbContext context, IUserContext userContext, IFileStorage fileStorage, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UploadMediaFileCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(UploadMediaFileCommand command, CancellationToken cancellationToken)
        {
            if (!await context.Users.AnyAsync(u => u.Id == command.OwnerId && u.Id == userContext.UserId))
            {
                return Result.Failure<Guid>(UserErrors.NotFound(command.OwnerId));
            }

            if (command.Size <= 0)
            {
                return Result.Failure<Guid>(MediaFileErrors.Empty);
            }

            if (command.Size > MediaFileRules.MaxFileSizeBytes)
            {
                return Result.Failure<Guid>(MediaFileErrors.TooLarge);
            }

            // The stored object used to be named ".webp" regardless of what was actually uploaded, and
            // anything at all was accepted here — including non-images.
            if (!MediaFileRules.IsAllowedContentType(command.ContentType))
            {
                return Result.Failure<Guid>(MediaFileErrors.ContentTypeNotAllowed);
            }

            Guid fileId = Guid.NewGuid();

            var objectKey = $"users/{userContext.UserId}/{fileId}{MediaFileRules.ExtensionFor(command.ContentType)}";

            try
            {
                await fileStorage.UploadAsync(
                    objectKey,
                    command.FileStream,
                    command.Bucket,
                    command.ContentType,
                    cancellationToken);
            }
            catch (FileStorageException)
            {
                return Result.Failure<Guid>(MediaFileErrors.StorageUnavailable);
            }

            MediaFile mediaFile = new MediaFile()
            {
                FileSize = (int)command.Size,
                FileType = MediaFileRules.TypeFor(command.ContentType),
                ObjectName = objectKey,
                Id = fileId,
                Bucket = command.Bucket,
                UploadDate = dateTimeProvider.UtcNow,
                UploaderId = userContext.UserId
            };

            context.MediaFiles.Add(mediaFile);

            await context.SaveChangesAsync(cancellationToken);

            return mediaFile.Id;
        }
    }
}
