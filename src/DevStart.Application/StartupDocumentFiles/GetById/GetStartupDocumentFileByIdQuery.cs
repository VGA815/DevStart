using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupDocumentFiles.GetById
{
    public sealed record GetStartupDocumentFileByIdQuery(Guid StartupDocumentFileId, int Expires) : IQuery<StartupDocumentFileResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.StartupDocumentFile(StartupDocumentFileId);

        public TimeSpan Expiration => TimeSpan.FromMinutes(Expires);
    }
}
