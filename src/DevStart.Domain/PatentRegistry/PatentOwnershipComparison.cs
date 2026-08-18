namespace DevStart.Domain.PatentRegistry
{
    /// <summary>
    /// Result of comparing the INN the startup declared with the INN of the record's rightsholder
    /// (SC-66). Deliberately named as a comparison, not as a confirmation: matching INNs say the
    /// declared legal entity is the one the register names as holder — nothing about whether the
    /// startup controls that entity. Proving control needs identity work this epic does not do.
    /// </summary>
    public enum PatentOwnershipComparison
    {
        /// <summary>No comparison possible: the startup declared no INN, or the dump carries none.</summary>
        NotComparable = 0,

        /// <summary>The rightsholder's INN equals the INN the startup declared.</summary>
        MatchesDeclaredInn = 1,

        /// <summary>The register names a different INN than the startup declared.</summary>
        DiffersFromDeclaredInn = 2,
    }
}
