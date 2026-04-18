using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupDocumentFiles.Upload
{
    internal sealed class UploadStartupDocumentFileCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider, IFileStorage fileStorage)
        : ICommandHandler<UploadStartupDocumentFileCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(UploadStartupDocumentFileCommand command, CancellationToken cancellationToken)
        {
            Startup? startup = await context.Startups.SingleOrDefaultAsync(s => s.Id == command.StartupId, cancellationToken);
            if (startup == null)
            {
                return Result.Failure<Guid>(StartupErrors.NotFound(command.StartupId));
            }

            StartupMember? startupMember = await context.StartupMembers.SingleOrDefaultAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId, cancellationToken);
            if (startupMember == null)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            Guid fileId = Guid.NewGuid();
            var objectKey = $"/startups/{command.StartupId}/{fileId}";

            await fileStorage.UploadAsync(objectKey, command.FileStream, command.Bucket, command.ContentType, cancellationToken);

            StartupDocumentFile startupDocumentFile = StartupDocumentFile.Create(
                fileId,
                command.StartupId,
                userContext.UserId,
                objectKey,
                command.Bucket,
                command.DocumentType,
                command.FileSize,
                command.DocumentName,
                dateTimeProvider.UtcNow);
            
            context.StartupDocumentFiles.Add(startupDocumentFile);
            await context.SaveChangesAsync(cancellationToken);

            return fileId;
        }
    }
}
