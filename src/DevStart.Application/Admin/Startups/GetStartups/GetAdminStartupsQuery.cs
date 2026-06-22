using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;

namespace DevStart.Application.Admin.Startups.GetStartups
{
    public sealed record GetAdminStartupsQuery(
        string? Search = null,
        bool? IsBanned = null,
        int PageNumber = 1,
        int PageSize = 50) : IQuery<List<AdminStartupListItemResponse>>;

    public sealed class AdminStartupListItemResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string PublicEmail { get; init; } = null!;
        public StartupStage Stage { get; init; }
        public bool IsStopped { get; init; }
        public bool IsBanned { get; init; }
        public string? BanReason { get; init; }
        public DateTime? BannedAt { get; init; }
        public DateTime? BanExpiresAt { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
