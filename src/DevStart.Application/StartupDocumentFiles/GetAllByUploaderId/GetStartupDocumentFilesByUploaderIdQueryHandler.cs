using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupDocumentFiles.GetAllByUploaderId
{
    internal sealed class GetStartupDocumentFilesByUploaderIdQueryHandler(IApplicationDbContext context, IUserContext userContext, IFileStorage fileStorage)
        : IQueryHandler<GetStartupDocumentFilesByUploaderIdQuery, List<StartupDocumentFileResponse>>
    {
        private const int PresignedUrlExpirySeconds = 3600;
        public async Task<Result<List<StartupDocumentFileResponse>>> Handle(GetStartupDocumentFilesByUploaderIdQuery query, CancellationToken cancellationToken)
        {
            // This lists one person's uploads across every startup they belong to, so it stays private to them.
            if (query.UploaderId != userContext.UserId)
            {
                return Result.Failure<List<StartupDocumentFileResponse>>(StartupDocumentFileErrors.Forbidden);
            }

            if (!await context.Users.AnyAsync(u => u.Id == query.UploaderId, cancellationToken))
            {
                return Result.Failure<List<StartupDocumentFileResponse>>(UserErrors.NotFound(query.UploaderId));
            }
            List<StartupDocumentFile> files = await context.StartupDocumentFiles
                .AsNoTracking()
                .Where(f => f.UploaderId == query.UploaderId)
                .ToListAsync(cancellationToken);

            StartupDocumentFileResponse[] responses;
            try
            {
                responses = await Task.WhenAll(
                    files.Select(async f =>
                    {
                        string presignedUrl = await fileStorage.GetPresignedUrl(
                            f.ObjectName,
                            f.Bucket,
                            PresignedUrlExpirySeconds,
                            cancellationToken);

                        return new StartupDocumentFileResponse
                        {
                            Id = f.Id,
                            StartupId = f.StartupId,
                            UploaderId = f.UploaderId,
                            DocumentName = f.DocumentName,
                            DocumentType = f.DocumentType,
                            FileSize = f.FileSize,
                            PresignedUrl = presignedUrl,
                            UploadDate = f.UploadDate
                        };
                    }));
            }
            catch (FileStorageException)
            {
                return Result.Failure<List<StartupDocumentFileResponse>>(StartupDocumentFileErrors.StorageUnavailable);
            }

            return responses.ToList();
        }
    }
}
