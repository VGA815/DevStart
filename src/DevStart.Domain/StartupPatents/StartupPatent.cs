using System.Text;
using DevStart.SharedKernel;

namespace DevStart.Domain.StartupPatents
{
    /// <summary>
    /// One claimed IP record of a startup: "we hold <i>this</i> certificate", as opposed to
    /// <c>Startup.HasPatents</c>, which claims only "we hold IP". The two coexist deliberately — a US
    /// patent or know-how is real IP with no Russian registry entry, so deriving the checkbox from the
    /// record count would punish it (docs/scoring-methodology.md).
    ///
    /// The record carries no verification state: the register is stored locally, so whether a number
    /// resolves is a join at read time, not a column that can go stale (SC-64).
    /// </summary>
    public sealed class StartupPatent : Entity
    {
        /// <summary>
        /// Upper bound on IP records per startup. Nothing scores off the count — the limit exists so
        /// the visible list of non-matches (SC-64 never hides them) cannot be buried under a hundred
        /// guessed numbers.
        /// </summary>
        public const int MaxPerStartup = 30;

        /// <summary>First year of the computer-program register — the floor for a certificate year.</summary>
        private const int FirstCertificateYear = 1993;

        public Guid Id { get; set; }
        public Guid StartupId { get; set; }
        public IntellectualPropertyKind Kind { get; set; }

        /// <summary>The number exactly as typed, kept so the card shows what the founder recognises.</summary>
        public string NumberRaw { get; set; } = null!;

        /// <summary>
        /// Digits only — the comparison key against the register and the per-startup dedup key.
        /// "RU 2 731 234 C1", "№2731234" and "2731234" all reduce to the same value.
        /// </summary>
        public string NumberNormalized { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public StartupPatent() { }

        public static StartupPatent Create(
            Guid startupId,
            IntellectualPropertyKind kind,
            string numberRaw,
            string numberNormalized,
            DateTime createdAt)
            => new()
            {
                Id = Guid.NewGuid(),
                StartupId = startupId,
                Kind = kind,
                NumberRaw = numberRaw.Trim(),
                NumberNormalized = numberNormalized,
                CreatedAt = createdAt
            };

        /// <summary>
        /// Reduces a number to its digits. A document reads "RU 2 731 234 C1": country code, number,
        /// publication kind — only the middle part identifies the record, the rest is decoration that
        /// the same record carries differently on a scan, on a ФИПС page and in a founder's memory.
        /// Returns <c>null</c> for anything holding a character that is neither a digit nor a known
        /// separator, so a null means "not comparable" rather than "duplicate of every other null".
        /// </summary>
        public static string? NormalizeNumber(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string value = raw.Trim().ToUpperInvariant();

            // Prefixes are stripped in a loop: "RU №2731234" carries two of them.
            bool stripped = true;
            while (stripped)
            {
                stripped = false;
                foreach (string prefix in Prefixes)
                {
                    if (value.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        value = value[prefix.Length..].TrimStart();
                        stripped = true;
                        break;
                    }
                }
            }

            // Trailing publication kind ("C1", "U1", "S"): a letter optionally followed by one digit.
            // It distinguishes the first publication from an amended one, not one record from another.
            if (value.Length >= 2 && char.IsDigit(value[^1]) && char.IsLetter(value[^2]))
            {
                value = value[..^2];
            }
            else if (value.Length >= 1 && char.IsLetter(value[^1]))
            {
                value = value[..^1];
            }

            var digits = new StringBuilder(value.Length);
            foreach (char c in value.TrimEnd())
            {
                if (char.IsDigit(c))
                {
                    digits.Append(c);
                }
                else if (!Separators.Contains(c))
                {
                    return null;
                }
            }

            return digits.Length == 0 ? null : digits.ToString();
        }

        /// <summary>
        /// Whether a normalized number has the shape its kind is issued in. This is a shape check, not
        /// a truth check: it turns a typo into a readable error at input instead of a "not found in the
        /// register" that reads like an accusation. Which record a number actually is — including
        /// whether the founder picked the right kind — is decided by the register itself.
        /// </summary>
        public static bool IsNumberWellFormed(IntellectualPropertyKind kind, string? normalized, int currentYear)
        {
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            switch (kind)
            {
                case IntellectualPropertyKind.Invention:
                    return normalized.Length == 7;

                case IntellectualPropertyKind.UtilityModel:
                case IntellectualPropertyKind.IndustrialDesign:
                case IntellectualPropertyKind.Trademark:
                    return normalized.Length is >= 5 and <= 7;

                case IntellectualPropertyKind.ComputerProgram:
                case IntellectualPropertyKind.Database:
                    // "2023612345" — registration year, then the '6' that marks a certificate series,
                    // then the sequence. The digit after the '6' separates programs from databases and
                    // is deliberately not enforced: a mis-picked kind is the register's answer to give
                    // ("not found"), not a format error to reject on.
                    return normalized.Length == 10
                        && normalized[4] == '6'
                        && int.TryParse(normalized[..4], out int year)
                        && year >= FirstCertificateYear
                        && year <= currentYear + 1;

                default:
                    return false;
            }
        }

        /// <summary>Human-readable expectation for a kind, used in the validation error.</summary>
        public static string NumberFormatHint(IntellectualPropertyKind kind) => kind switch
        {
            IntellectualPropertyKind.Invention => "7 цифр, например 2731234",
            IntellectualPropertyKind.UtilityModel => "5–7 цифр, например 213456",
            IntellectualPropertyKind.IndustrialDesign => "5–7 цифр, например 132456",
            IntellectualPropertyKind.ComputerProgram => "10 цифр вида «год + порядковый», например 2023612345",
            IntellectualPropertyKind.Database => "10 цифр вида «год + порядковый», например 2023621234",
            IntellectualPropertyKind.Trademark => "5–7 цифр, например 812345",
            _ => "только цифры номера",
        };

        private static readonly string[] Prefixes = ["RU", "РУ", "РФ", "№", "NO.", "N"];

        private static readonly HashSet<char> Separators =
            [' ', ' ', '-', '–', '—', '.', ',', '/', '\\', '№'];
    }
}
