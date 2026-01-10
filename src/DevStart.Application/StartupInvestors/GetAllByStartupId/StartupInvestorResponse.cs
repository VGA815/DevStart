namespace DevStart.Application.StartupInvestors.GetAllByStartupId
{
    public sealed class StartupInvestorResponse
    {
        public Guid StartupId { get; set; }
        public Guid ProfileId { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}