using DevStart.SharedKernel;

namespace DevStart.Domain.Notifications
{
    public sealed class Notification : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public Guid? ReferenceId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public Notification() {}
        public static Notification Create(Guid userId, NotificationType type, string title, string body, DateTime createdAt, Guid? referenceId = null)
        {
            return new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                ReferenceId = referenceId,
                IsRead = false,
                CreatedAt = createdAt
            };
        }
        public void MarkAsRead()
        {
            IsRead = true;
        }
    }
}
