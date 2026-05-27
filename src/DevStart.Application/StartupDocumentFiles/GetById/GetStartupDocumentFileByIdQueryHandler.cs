using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupDocumentFiles.GetById
{
    internal sealed class GetStartupDocumentFileByIdQueryHandler(IApplicationDbContext context, IFileStorage fileStorage)
        : IQueryHandler<GetStartupDocumentFileByIdQuery, StartupDocumentFileResponse>
    {
        public async Task<Result<StartupDocumentFileResponse>> Handle(GetStartupDocumentFileByIdQuery query, CancellationToken cancellationToken)
        {
            StartupDocumentFile? startupDocumentFile = await context.StartupDocumentFiles.SingleOrDefaultAsync(sdf => sdf.Id == query.StartupDocumentFileId, cancellationToken);

            if (startupDocumentFile == null)
            {
                return Result.Failure<StartupDocumentFileResponse>(StartupDocumentFileErrors.NotFound(query.StartupDocumentFileId));
            }

            string presignedUrl;
            try
            {
                presignedUrl = await fileStorage.GetPresignedUrl(startupDocumentFile.ObjectName, startupDocumentFile.Bucket, query.Expires, cancellationToken);
            }
            catch (FileStorageException ex)
            {
                return Result.Failure<StartupDocumentFileResponse>(
                    ex.NotFound ? StartupDocumentFileErrors.NotFound(query.StartupDocumentFileId) : StartupDocumentFileErrors.StorageUnavailable);
            }

            StartupDocumentFileResponse startup = new()
            {
                DocumentName = startupDocumentFile.DocumentName,
                DocumentType = startupDocumentFile.DocumentType,
                Id = query.StartupDocumentFileId,
                FileSize = startupDocumentFile.FileSize,
                PresignedUrl = presignedUrl,
                StartupId = startupDocumentFile.StartupId,
                UploaderId = startupDocumentFile.UploaderId,
            };

            return startup;
        }
    }
}
