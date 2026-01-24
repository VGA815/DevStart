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
            Guid fileId = Guid.NewGuid();

            var objectKey = $"/users/{userContext.UserId}/{fileId}.webp";
            
            MediaFile mediaFile = new MediaFile()
            {
                FileSize = (int)command.Size,
                FileType = MediaFileType.Img,
                FileUrl = objectKey,
                Id = fileId,
                Name = command.Bucket,
                UploadDate = dateTimeProvider.UtcNow,
                UploaderId = userContext.UserId
            };

            await fileStorage.UploadAsync(
                objectKey,
                command.FileStream,
                command.Bucket,
                command.ContentType,
                cancellationToken);

            context.MediaFiles.Add(mediaFile);

            await context.SaveChangesAsync(cancellationToken);

            return mediaFile.Id;
        }
    }
}
