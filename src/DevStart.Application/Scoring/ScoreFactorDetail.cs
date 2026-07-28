namespace DevStart.Application.Scoring
{
    /// <summary>
    /// How a raw value should be read. The backend never formats — no ₽, no %, no thousands
    /// separators cross the wire; the client owns units and locale.
    /// </summary>
    public enum ScoreValueKind
    {
        /// <summary>The formula reads this input, but it is not on file.</summary>
        None = 0,

        /// <summary>RUB amount, unformatted.</summary>
        Money = 1,

        /// <summary>Percentage points: 20 means 20%.</summary>
        Percent = 2,

        /// <summary>Integer count.</summary>
        Count = 3,

        /// <summary>Boolean, carried as 0/1.</summary>
        Flag = 4,

        /// <summary>An enumerated selection, carried in <see cref="ScoreValue.Code"/>.</summary>
        Code = 5,

        /// <summary>A reading on the same 0..100 points scale as the scores themselves.</summary>
        Score = 6,
    }

    /// <summary>
    /// One raw value inside the detail. <see cref="Code"/> is set only for
    /// <see cref="ScoreValueKind.Code"/>, and belongs to a separate vocabulary from the
    /// component/input/hint codes — it names a *value* (<c>stage.mvp</c>), not a rule.
    /// </summary>
    public sealed record ScoreValue(ScoreValueKind Kind, decimal? Number = null, string? Code = null)
    {
        public static ScoreValue Money(decimal value) => new(ScoreValueKind.Money, value);

        public static ScoreValue Percent(decimal value) => new(ScoreValueKind.Percent, value);

        public static ScoreValue Count(decimal value) => new(ScoreValueKind.Count, value);

        public static ScoreValue Flag(bool value) => new(ScoreValueKind.Flag, value ? 1m : 0m);

        public static ScoreValue Points(decimal value) => new(ScoreValueKind.Score, value);

        public static ScoreValue Of(string code) => new(ScoreValueKind.Code, Code: code);

        /// <summary>The input is read by the formula but has no value on file.</summary>
        public static readonly ScoreValue Absent = new(ScoreValueKind.None);
    }

    /// <summary>
    /// One addend of a factor score. The components of a factor sum to exactly that factor's score —
    /// including the scale-ceiling adjustment, which is itself a component with negative points
    /// (<c>&lt;factor&gt;.clamp</c>) rather than a flag, so the sum needs no special case.
    /// Order is part of the contract: base/tier first, bonuses in declaration order, clamp last.
    /// </summary>
    public sealed record ScoreComponent(string Code, decimal Points);

    /// <summary>A raw input the factor's formula actually read.</summary>
    public sealed record ScoreInput(string Code, ScoreValue Value);

    /// <summary>
    /// An unmet condition and what satisfying it is worth — <b>all else equal</b>. Hints are measured
    /// independently and are <b>not additive</b>: two 5-point hints on a factor with 5 points of
    /// headroom are each individually true and jointly impossible, so consumers must not sum them.
    /// <see cref="Points"/> is a delta in *factor* points (never total-score points — weight
    /// renormalization makes those non-linear), capped at the factor's headroom and always &gt; 0.
    /// When <see cref="EnablesFactor"/> is true the factor does not currently participate at all and
    /// <see cref="Points"/> is the score it *would* have; no delta is definable there, because
    /// bringing the factor back changes the renormalization rather than just the sub-score.
    /// </summary>
    public sealed record ScoreHint(
        string Code,
        decimal Points,
        IReadOnlyList<ScoreValue> Targets,
        bool EnablesFactor = false);

    /// <summary>
    /// Why a factor scored what it scored: the addends, the raw values the formula read, and the
    /// unmet conditions worth points. Codes are stable identifiers — labels are the client's job.
    /// See docs/scoring-methodology.md for the code taxonomy and its append-only policy.
    /// </summary>
    public sealed record ScoreFactorDetail(
        IReadOnlyList<ScoreComponent> Components,
        IReadOnlyList<ScoreInput> Inputs,
        IReadOnlyList<ScoreHint> Hints)
    {
        public static readonly ScoreFactorDetail Empty = new([], [], []);
    }
}
