using System.Globalization;
using System.Text;
using DevStart.Domain.PatentRegistry;
using DevStart.Domain.StartupPatents;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;

namespace DevStart.Application.PatentRegistry
{
    /// <summary>
    /// Parses a Rospatent open-data dump (CSV) into registry rows.
    ///
    /// Same two rules as the Damodaran import, for the same reasons. <b>Columns are found by header,
    /// never by index</b> — the open-data layout differs between registers and between releases, and a
    /// positional read would keep "working" while reading the wrong column. <b>An unrecognised layout
    /// is a refusal</b>, naming what was expected against what was found, with nothing written.
    ///
    /// The kind is supplied by the caller rather than read from the file: open data ships one register
    /// per dataset, so the file says which numbers it holds only by being that file.
    ///
    /// Rows that fail on their own (an unusable number, a shape that does not match the kind) are
    /// skipped and counted, not fatal — one malformed line must not cost the other quarter million
    /// theirs.
    /// </summary>
    public static class RospatentDumpParser
    {
        private static readonly string[] NumberHeaders =
        [
            "регистрационныйномер", "номергосударственнойрегистрации", "номерсвидетельства",
            "номерпатента", "номердокумента", "номеррегистрации", "номер",
            "registrationnumber", "regnumber", "number", "docnumber",
        ];

        private static readonly string[] TitleHeaders =
        [
            "название", "наименование", "названиеобъекта", "названиепрограммыдляэвм",
            "названиебазыданных", "наименованиеизобретения", "названиеизобретения",
            "title", "name",
        ];

        private static readonly string[] HolderHeaders =
        [
            "правообладатель", "правообладатели", "патентообладатель", "патентообладатели",
            "правообладательru", "holder", "holders", "patentholder",
        ];

        private static readonly string[] InnHeaders =
        [
            "инн", "иннправообладателя", "иннправообладателей", "holderinn", "inn",
        ];

        private static readonly string[] DateHeaders =
        [
            "датарегистрации", "датагосударственнойрегистрации", "датапубликации",
            "registrationdate", "regdate", "date",
        ];

        private static readonly string[] StatusHeaders =
        [
            "статус", "статусдокумента", "правовойстатус", "состояниеделопроизводства",
            "status", "legalstatus",
        ];

        private static readonly string[] DateFormats =
        [
            "dd.MM.yyyy", "yyyy-MM-dd", "dd/MM/yyyy", "yyyy.MM.dd", "dd.MM.yy",
        ];

        /// <summary>Rows scanned while hunting for the header — dumps can carry a preamble.</summary>
        private const int HeaderSearchLimit = 25;

        /// <summary>U+FEFF — the UTF-8 byte order mark exports lead with.</summary>
        private const char ByteOrderMark = '\uFEFF';

        /// <summary>U+200B — zero-width space, a frequent passenger in copy-pasted header cells.</summary>
        private const char ZeroWidthSpace = '\u200B';

        public static Result<PatentRegistryParseResult> Parse(
            string content, IntellectualPropertyKind kind, int currentYear)
        {
            char delimiter = DetectDelimiter(content);
            List<string[]> rows = ReadCsv(content.TrimStart(ByteOrderMark), delimiter);

            int headerIndex = -1;
            int numberColumn = -1;

            for (int i = 0; i < Math.Min(rows.Count, HeaderSearchLimit); i++)
            {
                int number = FindColumn(rows[i], NumberHeaders);
                if (number >= 0)
                {
                    headerIndex = i;
                    numberColumn = number;
                    break;
                }
            }

            if (headerIndex < 0)
            {
                return Result.Failure<PatentRegistryParseResult>(
                    PatentRegistryErrors.UnreadableDataset(
                        $"ожидалась колонка с номером ({string.Join(", ", NumberHeaders.Take(4))}…), "
                            + $"а найдено: {DescribeHeaders(rows)}"));
            }

            string[] header = rows[headerIndex];
            int titleColumn = FindColumn(header, TitleHeaders);
            int holderColumn = FindColumn(header, HolderHeaders);
            int innColumn = FindColumn(header, InnHeaders);
            int dateColumn = FindColumn(header, DateHeaders);
            int statusColumn = FindColumn(header, StatusHeaders);

            var parsed = new List<PatentRegistryRecord>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int skipped = 0;

            for (int i = headerIndex + 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (numberColumn >= row.Length)
                {
                    skipped++;
                    continue;
                }

                string? number = StartupPatent.NormalizeNumber(row[numberColumn]);
                if (!StartupPatent.IsNumberWellFormed(kind, number, currentYear))
                {
                    skipped++;
                    continue;
                }

                // A number twice in one file is one record filed twice; the first wins, so re-parsing
                // the same dump is deterministic.
                if (!seen.Add(number!))
                {
                    skipped++;
                    continue;
                }

                string? holder = Cell(row, holderColumn);

                parsed.Add(new PatentRegistryRecord(
                    kind,
                    number!,
                    Truncate(Cell(row, titleColumn), 1000),
                    Truncate(holder, 500),
                    // Co-owned records list several holders in one cell; the first valid ИНН is the one
                    // stored, and the full holder text stays visible next to it. A co-owner startup
                    // therefore compares as "differs" rather than as a match — the safe direction, since
                    // a wrong match is worse than a missing one.
                    FirstValidInn(Cell(row, innColumn)),
                    ParseDate(Cell(row, dateColumn)),
                    ParseStatus(Cell(row, statusColumn))));
            }

            if (parsed.Count == 0)
            {
                return Result.Failure<PatentRegistryParseResult>(PatentRegistryErrors.EmptyDataset);
            }

            return new PatentRegistryParseResult(parsed, skipped);
        }

        /// <summary>
        /// Russian exports are semicolon-separated as often as they are comma-separated, and neither
        /// spelling is announced anywhere in the file. The delimiter is whichever candidate wins over
        /// the opening lines — counted across several of them, not just the first, because a dump can
        /// open with a title line that holds no delimiter at all.
        /// </summary>
        private static char DetectDelimiter(string content)
        {
            using var reader = new StringReader(content.TrimStart(ByteOrderMark));

            int semicolons = 0;
            int commas = 0;
            int tabs = 0;
            int scanned = 0;

            string? line;
            while (scanned < HeaderSearchLimit && (line = reader.ReadLine()) is not null)
            {
                if (line.Trim().Length == 0)
                {
                    continue;
                }

                scanned++;
                semicolons += line.Count(c => c == ';');
                commas += line.Count(c => c == ',');
                tabs += line.Count(c => c == '\t');
            }

            if (semicolons > 0 && semicolons >= commas && semicolons >= tabs)
            {
                return ';';
            }

            return tabs > 0 && tabs > commas ? '\t' : ',';
        }

        private static string? Cell(string[] row, int column) =>
            column >= 0 && column < row.Length && row[column].Trim().Length > 0 ? row[column].Trim() : null;

        private static string? Truncate(string? value, int maxLength) =>
            value is null || value.Length <= maxLength ? value : value[..maxLength];

        /// <summary>First check-digit-valid ИНН in the cell, or <c>null</c>. Garbage is not stored.</summary>
        private static string? FirstValidInn(string? cell)
        {
            if (cell is null)
            {
                return null;
            }

            var candidate = new StringBuilder(12);
            foreach (char c in cell)
            {
                if (char.IsDigit(c))
                {
                    candidate.Append(c);
                    continue;
                }

                string? found = TakeIfValid(candidate);
                if (found is not null)
                {
                    return found;
                }

                candidate.Clear();
            }

            return TakeIfValid(candidate);
        }

        private static string? TakeIfValid(StringBuilder candidate)
        {
            if (candidate.Length is not (10 or 12))
            {
                return null;
            }

            string value = candidate.ToString();
            return RussianTaxId.IsValidInn(value) ? value : null;
        }

        private static DateOnly? ParseDate(string? cell)
        {
            if (cell is null)
            {
                return null;
            }

            return DateOnly.TryParseExact(
                cell, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
                ? parsed
                : null;
        }

        /// <summary>
        /// Maps the status text to the three states protection can be in. Order matters: "действие
        /// патента прекращено досрочно" holds all three words, and the most specific reading is the
        /// true one. Anything unrecognised stays <see cref="PatentProtectionStatus.Unknown"/> rather
        /// than being optimistically read as active.
        /// </summary>
        private static PatentProtectionStatus ParseStatus(string? cell)
        {
            if (cell is null)
            {
                return PatentProtectionStatus.Unknown;
            }

            string text = cell.ToLowerInvariant();

            if (text.Contains("досрочно", StringComparison.Ordinal))
            {
                return PatentProtectionStatus.EarlyTerminated;
            }

            if (text.Contains("прекращ", StringComparison.Ordinal)
                || text.Contains("истёк", StringComparison.Ordinal)
                || text.Contains("истек", StringComparison.Ordinal)
                || text.Contains("terminated", StringComparison.Ordinal))
            {
                return PatentProtectionStatus.Terminated;
            }

            if (text.Contains("действ", StringComparison.Ordinal)
                || text.Contains("active", StringComparison.Ordinal))
            {
                return PatentProtectionStatus.Active;
            }

            return PatentProtectionStatus.Unknown;
        }

        private static int FindColumn(string[] row, string[] candidates)
        {
            for (int i = 0; i < row.Length; i++)
            {
                string normalized = Normalize(row[i]);
                if (normalized.Length > 0 && Array.IndexOf(candidates, normalized) >= 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Lower-cased without whitespace, punctuation or invisible characters.</summary>
        private static string Normalize(string header)
        {
            var builder = new StringBuilder(header.Length);
            foreach (char c in header)
            {
                if (char.IsWhiteSpace(c) || char.IsControl(c) || c is ByteOrderMark or ZeroWidthSpace
                    || c is '_' or '-' or '.' or '"' or '\'' or '(' or ')')
                {
                    continue;
                }

                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }

        private static string DescribeHeaders(List<string[]> rows)
        {
            List<string> preview = rows
                .Take(HeaderSearchLimit)
                .Where(r => r.Any(c => c.Trim().Length > 0))
                .Take(3)
                .Select(r => string.Join(", ", r.Take(12).Select(c => c.Trim()).Where(c => c.Length > 0)))
                .ToList();

            return preview.Count == 0 ? "(пустой файл)" : string.Join(" | ", preview);
        }

        /// <summary>
        /// Minimal RFC 4180 reader with a configurable delimiter: quoted fields, doubled quotes inside
        /// them, CR/LF inside them.
        /// </summary>
        private static List<string[]> ReadCsv(string content, char delimiter)
        {
            var rows = new List<string[]>();
            var fields = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < content.Length && content[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\r')
                {
                    // Swallowed: the line ends on the '\n' that follows.
                }
                else if (c == '\n')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    rows.Add([.. fields]);
                    fields.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            if (field.Length > 0 || fields.Count > 0)
            {
                fields.Add(field.ToString());
                rows.Add([.. fields]);
            }

            return rows;
        }
    }
}
