using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.GetByEmail
{
    public sealed record GetUserByEmailQuery(string Email) : IQuery<UserResponse>;
}
