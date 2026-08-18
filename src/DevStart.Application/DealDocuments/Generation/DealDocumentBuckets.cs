namespace DevStart.Application.DealDocuments.Generation
{
    public static class DealDocumentBuckets
    {
        public const string Templates = "templates";
        public const string DealDocuments = "deal-documents";

        public static string TermSheetTemplateKey(string instrumentSlug) => $"term-sheet-{instrumentSlug}.md";

        public static string TermSheetObjectKey(Guid dealId) => $"deal-documents/{dealId}/term-sheet.md";

        public static string TermSheetPdfObjectKey(Guid dealId) => $"deal-documents/{dealId}/term-sheet.pdf";

        public static string CapTableObjectKey(Guid dealId) => $"deal-documents/{dealId}/cap-table.json";
    }
}
