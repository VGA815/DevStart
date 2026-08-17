using DevStart.Domain.Startups;

namespace DevStart.Application.Scoring.Benchmarks
{
    /// <summary>
    /// Curated ranking of the sectors by how crowded they are for a new entrant, and the rule that
    /// spreads that ranking onto the 0..100 scale the scoring engine reads.
    ///
    /// <b>Why a ranking and not a measurement.</b> "Density 62" has no absolute meaning. Counting active
    /// legal entities per ОКВЭД and computing revenue concentration would produce a number, but the
    /// number would still only be interpretable *relative to the other sectors* — so the honest output
    /// of either approach is a ranking. A ranking entered by hand, with its basis written down, is
    /// defensible to exactly the same degree as one computed from ЕГРЮЛ, and costs an evening instead of
    /// an integration. That is the trade this table makes deliberately.
    ///
    /// <b>The consequence, recorded on purpose.</b> Because the scale is relative, adding a sector to
    /// <see cref="Industry"/> shifts every value. The rank denominator ("rank 2 of 9") therefore travels
    /// in the source string: a tenth sector makes every existing row visibly describe a different scale,
    /// which is what append-only versioning is for.
    ///
    /// Suggestions built from this table are flagged <c>параметр</c>, never <c>выведено</c> — it is a
    /// judgement, and the output says so.
    /// </summary>
    public static class CompetitionIntensityRanking
    {
        /// <summary>Value of the most crowded sector. Not 100: the extremes would be false precision.</summary>
        private const decimal TopValue = 90m;

        /// <summary>Gap between adjacent ranks. Nine ranks span 90 down to 26.</summary>
        private const decimal Step = 8m;

        /// <summary>
        /// Sectors ordered from most to least crowded for a new entrant, each with the basis for its
        /// place. Order is the data here — the values fall out of <see cref="ValueForRank"/>.
        /// </summary>
        public static readonly IReadOnlyList<(Industry Industry, string Basis)> Ranking =
        [
            (Industry.Ecommerce, "низкие барьеры входа, доминирование маркетплейсов, множество мелких игроков"),
            (Industry.Saas, "много нишевых игроков, короткий цикл продукта, слабая защищённость ниши"),
            (Industry.Edtech, "низкий порог запуска, высокая текучесть игроков, слабая дифференциация"),
            (Industry.Ai, "быстрый приток новых команд, но рынок растёт быстрее числа игроков"),
            (Industry.Marketplace, "игроков немного, но сетевые эффекты лидеров делают вход тяжёлым"),
            (Industry.Other, "сборный сектор: срединное значение, отдельного основания нет"),
            (Industry.Fintech, "регуляторные барьеры и лицензирование ограничивают число игроков"),
            (Industry.Hardware, "капиталоёмкость и производственный цикл отсекают большинство входов"),
            (Industry.Biotech, "длинные циклы разработки и регистрации, игроков мало"),
        ];

        /// <summary>The spread rule, stated in the same words that go into the source string.</summary>
        public static string SpreadRule(int rankCount) =>
            $"значение = {TopValue:0} − (ранг−1)×{Step:0}, шкала "
            + $"{ValueForRank(rankCount):0}…{TopValue:0}, "
            + "крайние 0/100 не используются как ложная точность";

        /// <summary>Value for a 1-based rank. Clamped into 0..100 so the command validator can never reject it.</summary>
        public static decimal ValueForRank(int rank) =>
            Math.Clamp(TopValue - ((rank - 1) * Step), 0m, 100m);
    }
}
