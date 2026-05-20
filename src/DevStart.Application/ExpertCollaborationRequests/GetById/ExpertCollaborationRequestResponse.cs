using DevStart.Domain.ExpertCollaborationRequests;

namespace DevStart.Application.ExpertCollaborationRequests.GetById
{
    public sealed class ExpertCollaborationRequestResponse
    {
        public Guid Id { get; init; }
        public Guid ExpertProfileId { get; init; }
        public string ExpertDisplayName { get; init; } = string.Empty;
        public Guid StartupId { get; init; }
        public string StartupName { get; init; } = string.Empty;
        public CollaborationType CollaborationType { get; init; }
        public string? Message { get; init; }
        public int? ProposedHoursPerWeek { get; init; }
        public decimal? ProposedRate { get; init; }
        public ExpertCollaborationRequestStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
