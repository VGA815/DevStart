using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.MediaFiles;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.MediaFiles.GetById
{
    internal sealed class GetMediaFileByIdQueryHandler(IApplicationDbContext context, IFileStorage fileStorage, IUserContext userContext)
        : IQueryHandler<GetMediaFileByIdQuery, MediaFileResponse>
    {
        public async Task<Result<MediaFileResponse>> Handle(GetMediaFileByIdQuery query, CancellationToken cancellationToken)
        {
            MediaFile? mediaFile = await context.MediaFiles.SingleOrDefaultAsync(mf => mf.Id == query.FileId);

            if (mediaFile == null)
            {
                return Result.Failure<MediaFileResponse>(MediaFileErrors.NotFound(query.FileId));
            }

            string presignedUrl = await fileStorage.GetPresignedUrl(mediaFile.ObjectName, mediaFile.Bucket, query.Expires, cancellationToken);

            MediaFileResponse response = new MediaFileResponse()
            {
                FileSize = mediaFile.FileSize,
                Id = query.FileId,
                PresignedUrl = presignedUrl,
                UploadDate = mediaFile.UploadDate,
                UploaderId = mediaFile.UploaderId
            };

            return response;
        }
    }
}
