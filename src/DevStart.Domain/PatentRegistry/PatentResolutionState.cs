namespace DevStart.Domain.PatentRegistry
{
    /// <summary>
    /// How a claimed record stands against the local copy of the register. All three states are shown
    /// to the reader (SC-64): a hidden non-match would make "enter twenty numbers, show the three that
    /// stick" a working tactic, and merging the last two would let a platform-side gap read as a
    /// statement about the startup.
    /// </summary>
    public enum PatentResolutionState
    {
        /// <summary>The register for this kind of object has no rows loaded — the platform cannot check.</summary>
        RegistryUnavailable = 0,

        /// <summary>The register is loaded and holds this number.</summary>
        Found = 1,

        /// <summary>The register is loaded and does not hold this number.</summary>
        NotFoundInRegistry = 2,
    }
}
