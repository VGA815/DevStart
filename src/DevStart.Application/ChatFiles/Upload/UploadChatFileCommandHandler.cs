using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ChatFiles;
using DevStart.SharedKernel;

namespace DevStart.Application.ChatFiles.Upload
{
    internal sealed class UploadChatFileCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IFileStorage fileStorage,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UploadChatFileCommand, ChatFileResponse>
    {
        private const int PresignedUrlExpirySeconds = 3600;

        public async Task<Result<ChatFileResponse>> Handle(UploadChatFileCommand command, CancellationToken cancellationToken)
        {
            if (command.FileSize <= 0)
            {
                return Result.Failure<ChatFileResponse>(ChatFileErrors.Empty);
            }

            if (command.FileSize > ChatFileRules.MaxFileSizeBytes)
            {
                return Result.Failure<ChatFileResponse>(ChatFileErrors.TooLarge);
            }

            if (!ChatFileRules.IsAllowedContentType(command.ContentType))
            {
                return Result.Failure<ChatFileResponse>(ChatFileErrors.ContentTypeNotAllowed);
            }

            Guid uploaderId = userContext.UserId;
            Guid fileId = Guid.NewGuid();

            string fileName = SanitizeFileName(command.FileName);
            string objectKey = $"chat/{uploaderId}/{fileId}{ExtensionOf(fileName)}";

            try
            {
                await fileStorage.UploadAsync(
                    objectKey,
                    command.FileStream,
                    ChatFileRules.Bucket,
                    command.ContentType,
                    cancellationToken);
            }
            catch (FileStorageException)
            {
                return Result.Failure<ChatFileResponse>(ChatFileErrors.StorageUnavailable);
            }

            ChatFile chatFile = ChatFile.Create(
                fileId,
                uploaderId,
                objectKey,
                ChatFileRules.Bucket,
                fileName,
                command.ContentType.Trim(),
                command.FileSize,
                dateTimeProvider.UtcNow);

            context.ChatFiles.Add(chatFile);
            await context.SaveChangesAsync(cancellationToken);

            string presignedUrl;
            try
            {
                presignedUrl = await fileStorage.GetPresignedUrl(
                    chatFile.ObjectName,
                    chatFile.Bucket,
                    PresignedUrlExpirySeconds,
                    cancellationToken);
            }
            catch (FileStorageException)
            {
                return Result.Failure<ChatFileResponse>(ChatFileErrors.StorageUnavailable);
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

        /// <summary>Keeps the display name only: no directory components, no control characters, bounded length.</summary>
        private static string SanitizeFileName(string fileName)
        {
            string name = fileName.Replace('\\', '/');
            int lastSlash = name.LastIndexOf('/');
            if (lastSlash >= 0)
            {
                name = name[(lastSlash + 1)..];
            }

            name = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                name = "file";
            }

            return name.Length > ChatFileRules.MaxFileNameLength
                ? name[..ChatFileRules.MaxFileNameLength]
                : name;
        }

        /// <summary>The object key keeps the extension so downloads land with a sensible name; anything exotic is dropped.</summary>
        private static string ExtensionOf(string fileName)
        {
            string extension = Path.GetExtension(fileName);

            if (extension.Length is < 2 or > 10)
            {
                return string.Empty;
            }

            return extension[1..].All(char.IsLetterOrDigit)
                ? extension.ToLowerInvariant()
                : string.Empty;
        }
    }
}
