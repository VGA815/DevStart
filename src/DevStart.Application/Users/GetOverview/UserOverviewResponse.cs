using DevStart.Application.ExpertProfiles.GetById;
using DevStart.Application.InvestorProfiles.GetById;
using DevStart.Application.Profiles.GetById;

namespace DevStart.Application.Users.GetOverview
{
    public sealed record UserOverviewResponse
    {
        public Guid Id { get; init; }
        public string Username { get; init; } = null!;

        // Owner-only: redacted to null for non-owners by GetUserOverviewQueryHandler.
        public string? Email { get; init; }

        // Each profile section is null when the user does not have that profile.
        public ProfileResponse? Profile { get; init; }
        public InvestorProfileResponse? InvestorProfile { get; init; }
        public ExpertProfileResponse? ExpertProfile { get; init; }

        public UserStatisticsResponse Statistics { get; init; } = new();
    }
}
