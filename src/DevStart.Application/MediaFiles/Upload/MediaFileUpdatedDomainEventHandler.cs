using DevStart.Application.Abstractions.Data;
using DevStart.Domain.MediaFiles;
using DevStart.SharedKernel;

namespace DevStart.Application.MediaFiles.Upload
{
    internal sealed class MediaFileUpdatedDomainEventHandler(ICacheService _cache) : IDomainEventHandler<MediaFileUpdatedDomainEvent>
    {
        public async Task Handle(MediaFileUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            string key = $"v1:mediafiles:{domainEvent.MediaFileId}";
            await _cache.RemoveAsync(key);
        }
    }
}
