namespace DevStart.Application.StartupEquity.Vesting
{
    /// <summary>
    /// Computes what fraction of a holder's equity has vested as of a given date. Pure and
    /// stateless — the same schedule always yields the same fraction for a date.
    /// </summary>
    public interface IVestingCalculator
    {
        /// <summary>
        /// Returns the vested fraction in <c>[0, 1]</c>. No schedule
        /// (<paramref name="vestingStartDate"/> or a non-positive <paramref name="vestingMonths"/>)
        /// ⇒ fully vested (<c>1</c>). Before the cliff ⇒ <c>0</c>. Otherwise linear:
        /// <c>min(1, monthsElapsed / vestingMonths)</c>.
        /// </summary>
        decimal VestedFraction(DateTime? vestingStartDate, int? vestingMonths, int? cliffMonths, DateTime asOf);
    }
}
