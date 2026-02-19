using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Profiles.GetById
{
    public sealed record GetProfileByIdQuery(Guid UserId) : IQuery<ProfileResponse>, ICacheableQuery
    {
        public string CacheKey => $"v1:profiles:{UserId}";

        public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
    }
}
