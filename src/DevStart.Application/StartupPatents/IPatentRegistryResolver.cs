using DevStart.Domain.PatentRegistry;
using DevStart.Domain.Registries;
using DevStart.Domain.StartupPatents;

namespace DevStart.Application.StartupPatents
{
    /// <summary>
    /// Resolves a startup's claimed IP records against the local copy of the register.
    ///
    /// Resolution happens on read, not in a job: the register is stored locally, so this is a join
    /// rather than a network call. Three consequences, all deliberate — there is no asynchronous
    /// verification status, nothing can go stale, and after a quarterly refresh a lapsed patent starts
    /// showing as lapsed without a single row of data migration (SC-64).
    /// </summary>
    public interface IPatentRegistryResolver
    {
        /// <summary>Every record of the startup with its state and, when comparable, its ИНН comparison.</summary>
        Task<StartupPatentResolution> ResolveAsync(Guid startupId, CancellationToken cancellationToken);

        /// <summary>
        /// Whether at least one record resolves in the register <i>and</i> names the ИНН the startup
        /// declared as its rightsholder — the condition that lights the "сверено с реестром" provenance
        /// flag (SC-65/66). It moves no number: the score and the valuation range are unchanged either
        /// way, which is what the invariance tests pin down.
        /// </summary>
        Task<bool> HasRegistryCheckedOwnershipAsync(
            Guid startupId, string? declaredInn, CancellationToken cancellationToken);
    }

    /// <summary>One claimed record with everything the reader is entitled to see about it.</summary>
    public sealed record ResolvedStartupPatent(
        Guid Id,
        IntellectualPropertyKind Kind,
        string NumberRaw,
        string NumberNormalized,
        DateTime CreatedAt,
        RegistryLookupState State,
        DeclaredValueComparison Ownership,
        string? Title,
        string? HolderName,
        string? HolderInn,
        DateOnly? RegisteredOn,
        PatentProtectionStatus? ProtectionStatus);

    /// <summary>
    /// The startup's records resolved as a set. <see cref="DeclaredInn"/> is carried so the reader sees
    /// what the comparison was made against — a declared value, never a proven one.
    /// </summary>
    public sealed record StartupPatentResolution(
        string? DeclaredInn,
        IReadOnlyList<ResolvedStartupPatent> Records);
}
