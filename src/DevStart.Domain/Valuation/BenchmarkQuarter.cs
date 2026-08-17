namespace DevStart.Domain.Valuation
{
    /// <summary>
    /// The quarter grid the benchmark pipeline is keyed on. Collection runs quarterly, observations are
    /// stamped with the start of the quarter they describe, and a derived benchmark takes effect from
    /// that same date — so a multiple derived from Q2 data is on file as valid from 1 April, not from
    /// whenever an admin got round to entering it.
    /// </summary>
    public static class BenchmarkQuarter
    {
        /// <summary>First instant (UTC) of the quarter containing <paramref name="moment"/>.</summary>
        public static DateTime StartOf(DateTime moment)
        {
            int firstMonthOfQuarter = ((moment.Month - 1) / 3 * 3) + 1;
            return new DateTime(moment.Year, firstMonthOfQuarter, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        /// <summary>Human label for a quarter start, e.g. "2026Q3". Goes into the benchmark's source string.</summary>
        public static string Label(DateTime quarterStart) =>
            $"{quarterStart.Year}Q{((quarterStart.Month - 1) / 3) + 1}";
    }
}
