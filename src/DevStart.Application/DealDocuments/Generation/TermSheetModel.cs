using DevStart.Domain.InvestmentApplications;

namespace DevStart.Application.DealDocuments.Generation
{
    /// <summary>
    /// The term sheet as data, not as text. Every value is typed the way the domain holds it —
    /// money as <see cref="decimal"/>, rates as fractions, dates as <see cref="DateTime"/>, the cap
    /// table as rows — so that a renderer can lay it out in whatever way its medium demands.
    /// <para>
    /// No field carries a pre-formatted string, and in particular none carries markdown. That is the
    /// point of the type: the PDF renderer must not have to parse a markdown table back into rows,
    /// and the two renderers must not each hold their own copy of the decisions about *what* the
    /// document says.
    /// </para>
    /// </summary>
    public sealed record TermSheetModel(
        InvestmentInstrument Instrument,
        string StartupName,
        string StartupStage,
        Guid DealId,
        Guid ApplicationId,
        decimal Amount,
        decimal? ValuationCap,
        decimal? DiscountFraction,
        decimal? InterestRateFraction,
        int? TermMonths,
        decimal? PreMoneyValuation,
        decimal LiquidationPreference,
        bool ProRataRights,
        decimal InvestorSharePct,
        decimal FoundersTotalAfterPct,
        IReadOnlyList<TermSheetCapTableRow> CapTable,
        IReadOnlyList<TermSheetFounder> Founders,
        IReadOnlyList<TermSheetWarning> Warnings,
        TermSheetScore Score,
        DateTime AsOf,
        DateTime GeneratedAt);

    /// <summary>One row of the post-deal cap table. Percentages are percentage points: 12.5 means 12.5%.</summary>
    public sealed record TermSheetCapTableRow(
        string PartyName,
        string PartyType,
        decimal SharePctBefore,
        decimal SharePctAfter,
        decimal VestedPctAfter);

    /// <summary>
    /// One founder's pre-deal stake and vesting. A founder with no explicit schedule has
    /// <see cref="VestingStartDate"/>, <see cref="VestingMonths"/> and <see cref="VestedPercentage"/>
    /// all <c>null</c> — the renderer states the platform's standard vesting instead of computed
    /// numbers, which is a difference in what is known, not in how it is written.
    /// </summary>
    public sealed record TermSheetFounder(
        string Name,
        decimal EquityPercentage,
        DateTime? VestingStartDate,
        int? VestingMonths,
        int? CliffMonths,
        decimal? VestedPercentage);

    /// <summary>A deal-terms warning raised by the cap-table calculation.</summary>
    public sealed record TermSheetWarning(string Code, string Severity, string Message);

    /// <summary>
    /// The platform score and the computed valuation range, as they should appear on the document.
    /// <para>
    /// <see cref="Available"/> is the composer's decision, not the renderer's: when scoring produced
    /// no methods the whole block is unavailable and every renderer must print "no data" rather than
    /// a fabricated 0/100 and ₽0. Leaving that judgement to the renderers is exactly how a markdown
    /// document and a PDF of the same deal end up disagreeing about whether the startup scored zero.
    /// </para>
    /// Individual nullable members mean the same thing one factor at a time:
    /// <see cref="Total"/> is <c>null</c> when no factor had data, <see cref="Competition"/> when the
    /// competition factor dropped out, <see cref="MethodologyVersion"/> when none was recorded.
    /// </summary>
    public sealed record TermSheetScore(
        bool Available,
        decimal? Total,
        decimal Team,
        decimal Market,
        decimal Product,
        decimal Traction,
        decimal? Competition,
        decimal ValuationLow,
        decimal ValuationHigh,
        IReadOnlyList<string> MethodsUsed,
        string? MethodologyVersion,
        DateTime CalculatedAt)
    {
        /// <summary>Scoring did not produce a usable result; the document states so instead of showing zeros.</summary>
        public static TermSheetScore Unavailable(DateTime calculatedAt) =>
            new(false, null, 0m, 0m, 0m, 0m, null, 0m, 0m, [], null, calculatedAt);
    }
}
