using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.MediaFiles;
using DevStart.SharedKernel;

namespace DevStart.Application.MediaFiles.Upload
{
    internal sealed class MediaFileUpdatedDomainEventHandler(ICacheService cache) : IDomainEventHandler<MediaFileUpdatedDomainEvent>
    {
        public Task Handle(MediaFileUpdatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
            cache.RemoveAsync(CacheKeys.MediaFile(domainEvent.MediaFileId), cancellationToken);
    }
}
