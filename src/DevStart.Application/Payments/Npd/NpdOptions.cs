namespace DevStart.Application.Payments.Npd
{
    /// <summary>
    /// Self-employed (НПД, ФЗ-422) income-limit settings. Bound from the "Npd" configuration section.
    /// The annual limit is a hard cap (ч. 2 ст. 4 п. 8 ФЗ-422); the warning fraction drives an admin
    /// alert before the cap is reached. The calendar year is evaluated in <see cref="IncomeTimeZone"/>.
    /// </summary>
    public sealed class NpdOptions
    {
        /// <summary>Annual gross-income limit in RUB (2.4M ₽ by law).</summary>
        public decimal AnnualIncomeLimit { get; set; } = 2_400_000m;

        /// <summary>Fraction of the limit at which admins are alerted (0.80 = 80% = 1.92M ₽).</summary>
        public decimal WarningThresholdFraction { get; set; } = 0.80m;

        /// <summary>IANA/Windows time-zone id used to draw calendar-year boundaries (НПД year is МСК).</summary>
        public string IncomeTimeZone { get; set; } = "Europe/Moscow";

        /// <summary>Absolute RUB amount at which the warning fires.</summary>
        public decimal WarningAmount => AnnualIncomeLimit * WarningThresholdFraction;
    }
}
