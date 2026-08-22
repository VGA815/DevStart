namespace DevStart.Domain.Registries
{
    /// <summary>
    /// How a value the startup declared stands against the local copy of an external register. The
    /// three states are the whole contract, and every register the platform copies gets exactly these
    /// three — the patent register was the first (SC-64), the ЕГРЮЛ/ОКВЭД one will be the second.
    ///
    /// All three are shown to the reader. A hidden non-match would make "enter twenty numbers, show
    /// the three that stick" a working tactic, and merging the last two would let a platform-side gap
    /// (<see cref="RegistryUnavailable"/>) read as a statement about the startup.
    ///
    /// <see cref="RegistryUnavailable"/> is decided per *kind* of record, not per table: "the trademark
    /// register is not loaded" is a separate and honest answer while the patent one is.
    /// </summary>
    public enum RegistryLookupState
    {
        /// <summary>The register for this kind of record has no rows loaded — the platform cannot check.</summary>
        RegistryUnavailable = 0,

        /// <summary>The register is loaded and holds this value.</summary>
        Found = 1,

        /// <summary>The register is loaded and does not hold this value.</summary>
        NotFoundInRegistry = 2,
    }
}
