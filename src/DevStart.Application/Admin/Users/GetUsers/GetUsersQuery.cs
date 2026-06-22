using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Users;

namespace DevStart.Application.Admin.Users.GetUsers
{
    public sealed record GetUsersQuery(
        string? Search = null,
        UserSystemRole? Role = null,
        bool? IsBanned = null,
        int PageNumber = 1,
        int PageSize = 50) : IQuery<List<AdminUserListItemResponse>>;

    public sealed class AdminUserListItemResponse
    {
        public Guid Id { get; init; }
        public string Username { get; init; } = null!;
        public string Email { get; init; } = null!;
        public UserSystemRole Role { get; init; }
        public bool IsVerified { get; init; }
        public bool IsBanned { get; init; }
        public string? BanReason { get; init; }
        public DateTime? BannedAt { get; init; }
        public DateTime? BanExpiresAt { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
