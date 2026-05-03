using DevStart.Application.Abstractions.Validation;

namespace DevStart.Application.DealDocuments.Generation
{
    public sealed record CapTableResult(
        IReadOnlyList<CapTableEntry> Entries,
        decimal InvestorSharePct,
        decimal FoundersTotalAfterPct,
        IReadOnlyList<DealTermsFlag> Warnings);
}
