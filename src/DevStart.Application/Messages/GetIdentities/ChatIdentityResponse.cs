namespace DevStart.Application.Messages.GetIdentities
{
    /// <summary>A startup the caller may write and read chat as.</summary>
    public sealed class ChatIdentityResponse
    {
        public Guid StartupId { get; set; }
        public string Name { get; set; } = null!;
        public Guid? AvatarId { get; set; }
    }
}
