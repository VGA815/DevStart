namespace DevStart.Domain.Registries
{
    /// <summary>
    /// Result of comparing a value the startup declared with the one the register carries — the ИНН of
    /// a patent's rightsholder against the ИНН on the profile (SC-66) today, the ОКВЭД behind the
    /// declared <c>Industry</c> next.
    ///
    /// Deliberately named as a comparison, not as a confirmation. Matching values say the register and
    /// the declaration agree — nothing about whether the startup controls what the register names.
    /// Proving control needs identity work no registry copy can do.
    /// </summary>
    public enum DeclaredValueComparison
    {
        /// <summary>No comparison possible: one side or the other has no value to compare.</summary>
        NotComparable = 0,

        /// <summary>The register's value equals the declared one.</summary>
        Matches = 1,

        /// <summary>The register carries a different value than the startup declared.</summary>
        Differs = 2,
    }
}
