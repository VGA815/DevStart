using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.DealDocuments.GetTermSheetDownloadUrl
{
    public sealed record GetTermSheetDownloadUrlQuery(Guid DealId) : IQuery<TermSheetDownloadUrlResponse>;

    public sealed class TermSheetDownloadUrlResponse
    {
        public Guid DealId { get; init; }
        public string Url { get; init; } = null!;
        public DateTime ExpiresAt { get; init; }
    }
}
