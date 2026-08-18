namespace DevStart.Application.Scoring
{
    /// <summary>
    /// Where a scoring factor's inputs came from. Flags, because a factor can rest on more than one
    /// source at once (competition combines the startup's own analysis with a sector benchmark).
    /// <see cref="None"/> means "no data" — the investor can see which points rest on self-declaration
    /// and which are backed by something the startup cannot edit.
    /// </summary>
    [Flags]
    public enum ScoreFactorSource
    {
        /// <summary>No data behind this factor.</summary>
        None = 0,

        /// <summary>Declared by the startup itself (team, TAM, metrics, competitor cards).</summary>
        SelfReported = 1,

        /// <summary>Derived from platform data the startup does not type in directly (e.g. roadmap items).</summary>
        PlatformDerived = 2,

        /// <summary>Taken from an external, admin-curated benchmark (<c>valuation_benchmark</c>).</summary>
        ExternalBenchmark = 4,

        /// <summary>
        /// Checked against the local copy of the Rospatent register: a claimed number resolves there and
        /// the rightsholder's ИНН equals the one the startup declared (SC-65/66).
        ///
        /// Named for what the platform did, not for what a reader might wish it meant. It is not
        /// "ownership verified": the register says who the rightsholder is, ЕГРЮЛ says that entity
        /// exists, and neither says the startup controls it. Claiming more here would be the same
        /// mistake as calling the range an "estimate" instead of a computed guide.
        ///
        /// It carries no points. The factor's score, the valuation range and the set of methods used
        /// are identical with and without it — pinned down by tests, because a numeric effect is the
        /// kind of thing that arrives by accident and silently.
        /// </summary>
        RegistryChecked = 8,
    }

    /// <summary>One factor's contribution to the total score.</summary>
    /// <param name="Factor">Factor name (Team, Market, Product, Traction, Competition).</param>
    /// <param name="Score">The 0..100 sub-score, or <c>null</c> when the factor had no data and did not participate.</param>
    /// <param name="Weight">Renormalized weight within the participating set (0 for a factor that dropped out).</param>
    /// <param name="Source">Provenance of the factor's inputs.</param>
    public sealed record ScoreFactorBreakdown(
        string Factor,
        decimal? Score,
        decimal Weight,
        ScoreFactorSource Source)
    {
        /// <summary>
        /// How the factor arrived at its score: the components (summing to exactly <see cref="Score"/>),
        /// the raw inputs the formula read, and the unmet conditions worth points.
        ///
        /// Deliberately an init-only property rather than a positional parameter: the query result is
        /// cached as JSON, and an entry written before this field existed deserializes to
        /// <see cref="ScoreFactorDetail.Empty"/> (the initializer) instead of <c>null</c> (what a
        /// missing constructor argument would bind to). Consumers therefore never see a null detail.
        /// </summary>
        public ScoreFactorDetail Detail { get; init; } = ScoreFactorDetail.Empty;
    }

    /// <summary>
    /// Result of the scoring engine plus the valuation range computed from it.
    /// <see cref="TotalScore"/> is <c>null</c> when no factor had data — an explicit "insufficient
    /// data" signal (consumers render N/A rather than a fabricated 0/100), mirroring
    /// <see cref="ValuationResult.InsufficientData"/>. <see cref="CompetitionScore"/> is <c>null</c>
    /// when the competition factor dropped out; <see cref="Factors"/> carries the per-factor scores,
    /// renormalized weights and provenance.
    /// </summary>
    public sealed record ScoreResult(
        decimal? TotalScore,
        decimal TeamScore,
        decimal MarketScore,
        decimal ProductScore,
        decimal TractionScore,
        decimal? CompetitionScore,
        decimal ValuationLow,
        decimal ValuationHigh,
        IReadOnlyList<string> MethodsUsed,
        DateTime CalculatedAt,
        decimal ValuationPoint = 0m,
        IReadOnlyList<ValuationBreakdown>? ValuationMethods = null,
        string MethodologyVersion = "")
    {
        /// <summary>
        /// Per-factor breakdown: sub-score, renormalized weight, provenance and the detail behind the
        /// number. Set by the scoring engine; empty only on an insufficient-data result.
        /// </summary>
        public IReadOnlyList<ScoreFactorBreakdown> Factors { get; init; } = [];

        /// <summary>No factor had data — the score is not computable, and 0 would be a fabrication.</summary>
        public static ScoreResult InsufficientData(DateTime calculatedAt) =>
            new(null, 0m, 0m, 0m, 0m, null, 0m, 0m, [], calculatedAt);
    }
}
