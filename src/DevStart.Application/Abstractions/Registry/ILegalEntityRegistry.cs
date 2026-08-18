namespace DevStart.Application.Abstractions.Registry
{
    /// <summary>
    /// Read-only lookup of a legal entity in ЕГРЮЛ by ИНН (SC-66). It answers three things and no
    /// more: does the entity exist, is it active, what is it called. It says nothing about whether the
    /// startup that declared this ИНН controls that entity — proving control needs identity work
    /// (corporate-domain mail, documents, the ЕГРЮЛ director matched against a verified user) which
    /// this epic does not attempt. Every wording downstream is held to that limit.
    /// </summary>
    public interface ILegalEntityRegistry
    {
        Task<LegalEntityLookup> LookupAsync(string inn, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Outcome of a lookup. "Unavailable" is kept apart from "not found" for the same reason the
    /// register resolution keeps them apart: one is a statement about the platform, the other about
    /// the entity, and merging them would let a missing integration read as a finding.
    /// </summary>
    public enum LegalEntityLookupState
    {
        /// <summary>No ЕГРЮЛ source is configured, or it could not be reached.</summary>
        Unavailable = 0,

        /// <summary>ЕГРЮЛ knows this ИНН.</summary>
        Found = 1,

        /// <summary>ЕГРЮЛ was reachable and has no entity with this ИНН.</summary>
        NotFound = 2,
    }

    public sealed record LegalEntityLookup(LegalEntityLookupState State, LegalEntityRecord? Record)
    {
        public static readonly LegalEntityLookup Unavailable = new(LegalEntityLookupState.Unavailable, null);

        public static readonly LegalEntityLookup NotFound = new(LegalEntityLookupState.NotFound, null);

        public static LegalEntityLookup Found(LegalEntityRecord record) =>
            new(LegalEntityLookupState.Found, record);
    }

    /// <param name="Inn">The ИНН that was looked up.</param>
    /// <param name="Name">Registered name as ЕГРЮЛ spells it.</param>
    /// <param name="IsActive">Whether the entity is currently active (not liquidated).</param>
    /// <param name="StatusText">Status as the source words it, when it gives one.</param>
    /// <param name="AsOf">Date the source's data is current to, when it gives one.</param>
    public sealed record LegalEntityRecord(
        string Inn,
        string Name,
        bool IsActive,
        string? StatusText,
        DateOnly? AsOf);
}
