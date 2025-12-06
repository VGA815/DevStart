using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Profiles.GetById
{
    public sealed record GetProfileByIdQuery(Guid UserId) : IQuery<ProfileResponse>;
}
