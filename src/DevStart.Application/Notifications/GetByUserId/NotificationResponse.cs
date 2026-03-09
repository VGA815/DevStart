namespace DevStart.Application.Notifications.GetByUserId
{
    public sealed class NotificationResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Type { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public Guid? ReferenceId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}