using System.Globalization;

namespace DevStart.Infrastructure.Documents
{
    /// <summary>
    /// Number, money and date formatting for the Russian-language PDFs.
    /// <para>
    /// The markdown term sheet formats on <see cref="CultureInfo.InvariantCulture"/> and keeps doing
    /// so — its bytes are pinned by a golden test, and it is the machine-and-editor-facing form. The
    /// PDF is what a person carries away and shows to someone else, so it reads as a Russian
    /// document: <c>1 500 000,00 ₽</c>, <c>17.05.2026</c>. The two documents therefore write the same
    /// amount differently; that is a deliberate, recorded choice, not drift.
    /// </para>
    /// </summary>
    internal static class RuFormat
    {
        internal static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("ru-RU");

        internal const string Dash = "—";

        /// <summary>What the document says where a value is genuinely not on file.</summary>
        internal const string NoData = "нет данных";

        /// <summary>Money with two decimals and the ruble sign after the number, as Russian usage puts it.</summary>
        internal static string Money(decimal value) => $"{value.ToString("N2", Culture)} ₽";

        internal static string Money(decimal? value) => value is { } v ? Money(v) : Dash;

        /// <summary>A percentage already expressed in percentage points: 12.5 renders as "12,5 %".</summary>
        internal static string Percent(decimal value) => $"{value.ToString("0.##", Culture)} %";

        internal static string Percent(decimal? value) => value is { } v ? Percent(v) : Dash;

        /// <summary>A fraction expressed as a percentage: 0.2 renders as "20 %".</summary>
        internal static string FractionAsPercent(decimal? value) =>
            value is { } v ? Percent(v * 100m) : Dash;

        /// <summary>A bare number with up to two decimals and no unit.</summary>
        internal static string Number(decimal value) => value.ToString("0.##", Culture);

        internal static string Multiplier(decimal value) => $"{value.ToString("0.#", Culture)}x";

        internal static string Date(DateTime value) => value.ToString("dd.MM.yyyy", Culture);

        internal static string Date(DateTime? value) => value is { } v ? Date(v) : Dash;

        internal static string Timestamp(DateTime value) =>
            value.ToString("dd.MM.yyyy HH:mm", Culture) + " UTC";

        internal static string YesNo(bool value) => value ? "Да" : "Нет";

        internal static string Months(int? value) =>
            value is { } v ? v.ToString(Culture) : Dash;
    }
}
