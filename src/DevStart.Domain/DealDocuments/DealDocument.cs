using DevStart.SharedKernel;

namespace DevStart.Domain.DealDocuments
{
    /// <summary>
    /// The generated document set for one deal: the markdown term sheet, the same term sheet as a
    /// PDF, and the cap-table JSON behind them.
    /// <para>
    /// There is exactly one row per deal, enforced by a unique index, and the generation job returns
    /// early when it finds one. That makes the set immutable in practice: <c>regenerateDocuments</c>
    /// re-runs the job, the job sees the row and stops, and nothing is overwritten. The endpoint is a
    /// repair path for a deal whose documents were never produced, not a way to replace documents
    /// that exist — a deliberate choice, recorded here because the name suggests otherwise.
    /// </para>
    /// </summary>
    public sealed class DealDocument : Entity
    {
        public Guid Id { get; set; }
        public Guid DealId { get; set; }
        public string TermSheetObjectKey { get; set; } = null!;
        public string TermSheetPdfObjectKey { get; set; } = null!;

        /// <summary>
        /// SHA-256 of the PDF bytes, lower-case hex. The renderer is deterministic, so this is a
        /// fingerprint of the document's content: a reader who hashes the file they hold gets the
        /// same value, and re-rendering the same deal reproduces it.
        /// </summary>
        public string TermSheetPdfSha256 { get; set; } = null!;

        public string CapTableObjectKey { get; set; } = null!;
        public DateTime GeneratedAt { get; set; }

        public DealDocument() { }

        public static DealDocument Create(
            Guid dealId,
            string termSheetObjectKey,
            string termSheetPdfObjectKey,
            string termSheetPdfSha256,
            string capTableObjectKey,
            DateTime utcNow) => new()
            {
                Id = Guid.NewGuid(),
                DealId = dealId,
                TermSheetObjectKey = termSheetObjectKey,
                TermSheetPdfObjectKey = termSheetPdfObjectKey,
                TermSheetPdfSha256 = termSheetPdfSha256,
                CapTableObjectKey = capTableObjectKey,
                GeneratedAt = utcNow
            };

        /// <summary>
        /// Backfills a row written before PDF generation existed. Those rows carry an empty PDF key,
        /// and without this they would keep it forever: the generation job stops as soon as it sees a
        /// row, so nothing would ever revisit them.
        /// <para>
        /// This is the one place a document set is rewritten, and only ever from "no PDF" to "PDF".
        /// It is not a versioning door: a complete set is still never regenerated.
        /// </para>
        /// </summary>
        public void AttachPdf(string termSheetPdfObjectKey, string termSheetPdfSha256, DateTime utcNow)
        {
            TermSheetPdfObjectKey = termSheetPdfObjectKey;
            TermSheetPdfSha256 = termSheetPdfSha256;
            GeneratedAt = utcNow;
        }

        /// <summary>True for a row written before PDF generation existed.</summary>
        public bool HasPdf => !string.IsNullOrEmpty(TermSheetPdfObjectKey);
    }
}
