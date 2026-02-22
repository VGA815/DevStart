using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Startups.GetById
{
    public sealed record GetStartupByIdQuery(Guid StartupId) : IQuery<StartupResponse>, ICacheableQuery
    {
        public string CacheKey => $"v1:startups:{StartupId}";

        public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
    }
}
