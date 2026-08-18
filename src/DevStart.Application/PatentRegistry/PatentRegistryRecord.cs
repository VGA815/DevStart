using DevStart.Domain.PatentRegistry;
using DevStart.Domain.StartupPatents;

namespace DevStart.Application.PatentRegistry
{
    /// <summary>One row of a parsed dump, before it meets the table.</summary>
    public sealed record PatentRegistryRecord(
        IntellectualPropertyKind Kind,
        string NumberNormalized,
        string? Title,
        string? HolderName,
        string? HolderInn,
        DateOnly? RegisteredOn,
        PatentProtectionStatus Status);

    /// <summary>
    /// Outcome of a parse. <see cref="SkippedRows"/> is reported rather than swallowed: a dump where
    /// most rows are unusable parses "successfully" into very little, and the counter is what makes
    /// that visible in the log and in the admin's response.
    /// </summary>
    public sealed record PatentRegistryParseResult(
        IReadOnlyList<PatentRegistryRecord> Records,
        int SkippedRows);
}
