namespace DevStart.Application.Users.GetByEmail
{
    public sealed record UserResponse
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = null!;
        public string Username { get; init; } = null!;
    }
}