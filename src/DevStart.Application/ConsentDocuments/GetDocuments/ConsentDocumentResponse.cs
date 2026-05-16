using DevStart.Domain.UserConsents;

namespace DevStart.Application.ConsentDocuments.GetDocuments
{
    public sealed class ConsentDocumentResponse
    {
        public Guid Id { get; set; }
        public ConsentType Type { get; set; }
        public string Version { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
