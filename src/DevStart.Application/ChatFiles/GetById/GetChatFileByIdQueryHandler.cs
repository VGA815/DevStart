using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Messages;
using DevStart.Domain.ChatFiles;
using DevStart.Domain.Messages;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ChatFiles.GetById
{
    internal sealed class GetChatFileByIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IFileStorage fileStorage)
        : IQueryHandler<GetChatFileByIdQuery, ChatFileResponse>
    {
        public async Task<Result<ChatFileResponse>> Handle(GetChatFileByIdQuery query, CancellationToken cancellationToken)
        {
            ChatFile? chatFile = await context.ChatFiles
                .AsNoTracking()
                .SingleOrDefaultAsync(f => f.Id == query.ChatFileId, cancellationToken);

            if (chatFile is null)
            {
                return Result.Failure<ChatFileResponse>(ChatFileErrors.NotFound(query.ChatFileId));
            }

            Guid userId = userContext.UserId;

            if (chatFile.UploaderId != userId)
            {
                // A file that has not been sent yet is visible to its uploader only; once sent, access
                // follows the message it was attached to.
                if (chatFile.MessageId is null)
                {
                    return Result.Failure<ChatFileResponse>(ChatFileErrors.Unauthorized);
                }

                Message? message = await context.Messages
                    .AsNoTracking()
                    .SingleOrDefaultAsync(m => m.Id == chatFile.MessageId.Value, cancellationToken);

                if (message is null || !await MessageAccess.CanReadAsync(context, message, userId, cancellationToken))
                {
                    return Result.Failure<ChatFileResponse>(ChatFileErrors.Unauthorized);
                }
            }

            string presignedUrl;
            try
            {
                presignedUrl = await fileStorage.GetPresignedUrl(
                    chatFile.ObjectName,
                    chatFile.Bucket,
                    query.Expires,
                    cancellationToken);
            }
            catch (FileStorageException ex)
            {
                return Result.Failure<ChatFileResponse>(
                    ex.NotFound ? ChatFileErrors.NotFound(query.ChatFileId) : ChatFileErrors.StorageUnavailable);
            }

            return new ChatFileResponse
            {
                Id = chatFile.Id,
                UploaderId = chatFile.UploaderId,
                FileName = chatFile.FileName,
                ContentType = chatFile.ContentType,
                FileSize = chatFile.FileSize,
                PresignedUrl = presignedUrl,
                UploadDate = chatFile.UploadDate,
            };
        }
    }
}
