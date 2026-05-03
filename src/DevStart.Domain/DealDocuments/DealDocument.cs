using DevStart.SharedKernel;

namespace DevStart.Domain.DealDocuments
{
    public sealed class DealDocument : Entity
    {
        public Guid Id { get; set; }
        public Guid DealId { get; set; }
        public string TermSheetObjectKey { get; set; } = null!;
        public string CapTableObjectKey { get; set; } = null!;
        public DateTime GeneratedAt { get; set; }

        public DealDocument() { }

        public static DealDocument Create(
            Guid dealId,
            string termSheetObjectKey,
            string capTableObjectKey,
            DateTime utcNow) => new()
            {
                Id = Guid.NewGuid(),
                DealId = dealId,
                TermSheetObjectKey = termSheetObjectKey,
                CapTableObjectKey = capTableObjectKey,
                GeneratedAt = utcNow
            };
    }
}
