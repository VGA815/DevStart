using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.MediaFiles.GetById
{
    public sealed record GetMediaFileByIdQuery(Guid FileId, int Expires) : IQuery<MediaFileResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.MediaFile(FileId);
        public TimeSpan Expiration => TimeSpan.FromMinutes(Expires);
    }
}
