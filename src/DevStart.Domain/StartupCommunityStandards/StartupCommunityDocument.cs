using DevStart.SharedKernel;

namespace DevStart.Domain.StartupCommunityStandards
{
    /// <summary>
    /// A community health document published by a startup (code of conduct, contributing guide, …).
    /// Markdown lives in the database, mirroring <c>ConsentDocument</c>.
    /// There are no drafts: a document that exists is published and public, so deleting it is how a
    /// startup un-publishes.
    /// </summary>
    public sealed class StartupCommunityDocument : Entity
    {
        public Guid Id { get; set; }
        public Guid StartupId { get; set; }
        public CommunityDocumentType Type { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;

        /// <summary>Profile that last wrote the document — the founder/administrator who saved it.</summary>
        public Guid AuthorId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public StartupCommunityDocument() { }

        public static StartupCommunityDocument Create(
            Guid startupId,
            CommunityDocumentType type,
            string title,
            string content,
            Guid authorId,
            DateTime createdAt)
            => new()
            {
                Id        = Guid.NewGuid(),
                StartupId = startupId,
                Type      = type,
                Title     = title,
                Content   = content,
                AuthorId  = authorId,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

        public void Update(string title, string content, Guid authorId, DateTime utcNow)
        {
            Title     = title;
            Content   = content;
            AuthorId  = authorId;
            UpdatedAt = utcNow;
        }
    }
}
