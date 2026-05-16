using DevStart.Domain.UserConsents;
using DevStart.SharedKernel;

namespace DevStart.Domain.ConsentDocuments
{
    public sealed class ConsentDocument : Entity
    {
        public Guid Id { get; set; }
        public ConsentType Type { get; set; }
        public string Version { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        public ConsentDocument() { }

        public static ConsentDocument Create(
            ConsentType type,
            string version,
            string title,
            string content,
            DateTime createdAt)
            => new()
            {
                Id        = Guid.NewGuid(),
                Type      = type,
                Version   = version,
                Title     = title,
                Content   = content,
                CreatedAt = createdAt,
                IsActive  = false
            };

        public void Activate()   => IsActive = true;
        public void Deactivate() => IsActive = false;
    }
}
