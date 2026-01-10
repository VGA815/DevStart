namespace DevStart.Application.StartupInvestors.GetById
{
    public sealed class StartupInvestorResponse
    {
        public Guid StartupId { get; init; }
        public Guid ProfileId { get; init; }
        public bool IsPublic { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}