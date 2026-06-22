using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.Users;

namespace DevStart.Application.Admin.Users.GetUserDetail
{
    public sealed record GetUserDetailQuery(Guid UserId) : IQuery<AdminUserDetailResponse>;

    public sealed class AdminUserDetailResponse
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
        public Guid? BannedByUserId { get; init; }
        public DateTime CreatedAt { get; init; }
        public AdminUserSubscriptionSummary? CurrentSubscription { get; init; }
    }

    public sealed class AdminUserSubscriptionSummary
    {
        public Guid Id { get; init; }
        public SubscriptionPlan Plan { get; init; }
        public SubscriptionStatus Status { get; init; }
        public SubscriptionSource Source { get; init; }
        public DateTime StartedAt { get; init; }
        public DateTime ExpiresAt { get; init; }
    }
}
