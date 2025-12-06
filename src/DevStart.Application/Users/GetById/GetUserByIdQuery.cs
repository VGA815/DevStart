using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.GetById
{
    public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserResponse>;
}
