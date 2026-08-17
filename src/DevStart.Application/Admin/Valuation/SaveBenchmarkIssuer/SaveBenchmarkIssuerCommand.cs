using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;

namespace DevStart.Application.Admin.Valuation.SaveBenchmarkIssuer
{
    /// <summary>
    /// Creates or edits a curated comparable. Unlike a benchmark this row is mutable: it is the
    /// instrument that produces a figure, and reproducibility rides on the derived benchmark's
    /// <c>Source</c>, not on versioning the instrument.
    ///
    /// <paramref name="Id"/> <c>null</c> creates; set updates. Retiring an issuer means
    /// <paramref name="IsActive"/> <c>false</c> — there is deliberately no delete, because observations
    /// already collected under it must keep their referent.
    /// </summary>
    public sealed record SaveBenchmarkIssuerCommand(
        Guid? Id,
        string Ticker,
        string? Inn,
        string DisplayName,
        Industry Industry,
        bool IsActive,
        decimal? RevenueOverride,
        int? RevenueOverrideFiscalYear,
        string? RevenueOverrideNote,
        string? Note) : ICommand<Guid>;
}
