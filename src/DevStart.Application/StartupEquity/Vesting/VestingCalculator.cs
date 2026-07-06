namespace DevStart.Application.StartupEquity.Vesting
{
    internal sealed class VestingCalculator : IVestingCalculator
    {
        public decimal VestedFraction(DateTime? vestingStartDate, int? vestingMonths, int? cliffMonths, DateTime asOf)
        {
            // No schedule on file ⇒ the holder owns their equity outright (nothing to vest).
            if (vestingStartDate is not { } start || vestingMonths is not { } months || months <= 0)
            {
                return 1m;
            }

            int monthsElapsed = WholeMonthsBetween(start, asOf);
            if (monthsElapsed <= 0)
            {
                return 0m;
            }

            // Cliff gate: nothing releases until the cliff is reached. At the cliff, the accrued
            // linear portion (monthsElapsed / months) vests at once; thereafter it accrues monthly.
            int cliff = cliffMonths ?? 0;
            if (monthsElapsed < cliff)
            {
                return 0m;
            }

            if (monthsElapsed >= months)
            {
                return 1m;
            }

            return (decimal)monthsElapsed / months;
        }

        // Number of whole calendar months elapsed from start to asOf (0 if asOf precedes start).
        private static int WholeMonthsBetween(DateTime start, DateTime asOf)
        {
            if (asOf <= start)
            {
                return 0;
            }

            int months = ((asOf.Year - start.Year) * 12) + asOf.Month - start.Month;
            if (asOf.Day < start.Day)
            {
                months--;
            }

            return months < 0 ? 0 : months;
        }
    }
}
