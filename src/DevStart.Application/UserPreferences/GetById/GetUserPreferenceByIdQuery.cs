using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.UserPreferences.GetById
{
    public sealed record GetUserPreferenceByIdQuery(Guid UserId) : IQuery<UserPreferenceResponse>, ICacheableQuery
    {
        public string CacheKey => $"v1:user-preferences:{UserId}";

        public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
    }
}
