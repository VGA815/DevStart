using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.UserPreferences.GetById
{
    public sealed record GetUserPreferenceByIdQuery(Guid UserId) : IQuery<UserPreferenceResponse>;
}
