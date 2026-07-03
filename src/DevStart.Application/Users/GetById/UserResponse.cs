using DevStart.Domain.Users;

namespace DevStart.Application.Users.GetById
{
    public sealed record UserResponse
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = null!;
        public string Username { get; init; } = null!;
        public bool IsVerified { get; init; }
        public UserSystemRole Role { get; init; }
    }
}
