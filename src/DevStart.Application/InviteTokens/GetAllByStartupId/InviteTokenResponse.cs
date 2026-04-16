namespace DevStart.Application.InviteTokens.GetAllByStartupId
{
    public sealed class InviteTokenResponse
    {
        public Guid Id { get; set; }
        public Guid StartupId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }
}