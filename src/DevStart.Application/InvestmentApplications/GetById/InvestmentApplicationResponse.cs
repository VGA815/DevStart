using DevStart.Domain.InvestmentApplications;

namespace DevStart.Application.InvestmentApplications.GetById
{
    public sealed class InvestmentApplicationResponse
    {
        public Guid Id { get; init; }
        public Guid InvestorProfileId { get; init; }
        public Guid StartupId { get; init; }
        public Guid? RoadmapItemId { get; init; }
        public decimal Amount { get; init; }
        public string? Message { get; init; }
        public InvestmentApplicationStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
