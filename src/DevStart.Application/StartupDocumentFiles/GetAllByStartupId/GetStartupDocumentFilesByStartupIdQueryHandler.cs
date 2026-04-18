using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupDocumentFiles.GetAllByStartupId
{
    internal sealed class GetStartupDocumentFilesByStartupIdQueryHandler(IApplicationDbContext context, IFileStorage fileStorage)
        : IQueryHandler<GetStartupDocumentFilesByStartupIdQuery, List<StartupDocumentFileResponse>>
    {
        private const int PresignedUrlExpirySeconds = 3600;

        public async Task<Result<List<StartupDocumentFileResponse>>> Handle(GetStartupDocumentFilesByStartupIdQuery query, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(s => s.Id == query.StartupId, cancellationToken))
            {
                return Result.Failure<List<StartupDocumentFileResponse>>(StartupErrors.NotFound(query.StartupId));
            }
            List<StartupDocumentFile> files = await context.StartupDocumentFiles
                .AsNoTracking()
                .Where(f => f.StartupId == query.StartupId)
                .ToListAsync(cancellationToken);

            StartupDocumentFileResponse[] responses = await Task.WhenAll(
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

            return responses.ToList();
        }
    }
}
