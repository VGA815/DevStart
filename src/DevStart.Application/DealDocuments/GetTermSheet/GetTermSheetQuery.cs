using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.DealDocuments.GetTermSheet
{
    public sealed record GetTermSheetQuery(Guid DealId) : IQuery<TermSheetResponse>;

    public sealed class TermSheetResponse
    {
        public Guid DealId { get; init; }
        public string Markdown { get; init; } = null!;
        public DateTime GeneratedAt { get; init; }
    }
}
