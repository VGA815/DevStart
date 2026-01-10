namespace DevStart.Application.StartupFollowers.GetAllByStartupId
{
    public sealed class StartupFollowerResponse
    {
        public Guid StartupId { get; set; }
        public Guid ProfileId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}