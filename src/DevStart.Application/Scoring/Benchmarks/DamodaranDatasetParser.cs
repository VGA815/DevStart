using System.Globalization;
using System.Text;
using DevStart.Application.Abstractions.Valuation;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;

namespace DevStart.Application.Scoring.Benchmarks
{
    /// <summary>
    /// Parses a Damodaran industry dataset (CSV export) into EV/Sales observations.
    ///
    /// Two rules define this parser. <b>Columns are found by header, never by index</b> — the layout
    /// shifts from one annual release to the next, and a positional read would keep "working" while
    /// silently reading the wrong column. And <b>the parse is all or nothing</b>: an unrecognised layout
    /// yields a readable error naming what was expected against what was found, and zero rows written.
    /// A partial import is worse than a refusal, because the result is a plausible but incomplete set
    /// that nothing downstream can tell apart from a complete one.
    ///
    /// CSV rather than the original .xls on purpose: exporting the sheet is a ten-second step, and it
    /// keeps the import to a format whose parse an auditor can follow. The error message says so when
    /// the layout cannot be recognised.
    /// </summary>
    public static class DamodaranDatasetParser
    {
        /// <summary>Header spellings seen across releases for the industry-name column.</summary>
        private static readonly string[] IndustryHeaders =
            ["industryname", "industry", "industrygroup", "sector"];

        /// <summary>Header spellings for the EV/Sales column.</summary>
        private static readonly string[] ValueHeaders =
            ["ev/sales", "evsales", "ev/salesratio", "ev/revenues", "evrevenues"];

        /// <summary>Rows scanned while hunting for the header — the files carry a title preamble.</summary>
        private const int HeaderSearchLimit = 25;

        /// <summary>U+FEFF — the UTF-8 byte order mark spreadsheet exports lead with.</summary>
        private const char ByteOrderMark = '\uFEFF';

        /// <summary>U+200B — zero-width space, a frequent passenger in copy-pasted header cells.</summary>
        private const char ZeroWidthSpace = '\u200B';

        public static Result<List<DamodaranBucketObservation>> Parse(string content)
        {
            // Spreadsheet exports habitually lead with a UTF-8 BOM. A reader configured to detect it
            // strips it, but this parser is public and unit-tested on raw strings, so it must not
            // depend on how its caller built the text — a stray U+FEFF glued to the first header cell
            // would otherwise surface as "layout unrecognised" on a perfectly good file.
            List<string[]> rows = ReadCsv(content.TrimStart(ByteOrderMark));

            int headerIndex = -1;
            int industryColumn = -1;
            int valueColumn = -1;

            for (int i = 0; i < Math.Min(rows.Count, HeaderSearchLimit); i++)
            {
                int industry = FindColumn(rows[i], IndustryHeaders);
                int value = FindColumn(rows[i], ValueHeaders);

                if (industry >= 0 && value >= 0)
                {
                    headerIndex = i;
                    industryColumn = industry;
                    valueColumn = value;
                    break;
                }
            }

            if (headerIndex < 0)
            {
                return Result.Failure<List<DamodaranBucketObservation>>(
                    ValuationBenchmarkErrors.DamodaranLayoutUnrecognised(
                        $"an industry column ({string.Join(", ", IndustryHeaders)}) "
                            + $"and an EV/Sales column ({string.Join(", ", ValueHeaders)})",
                        DescribeHeaders(rows)));
            }

            var parsed = new List<DamodaranBucketObservation>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = headerIndex + 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (industryColumn >= row.Length || valueColumn >= row.Length)
                {
                    continue;
                }

                string bucket = row[industryColumn].Trim();
                if (bucket.Length == 0 || bucket.Length > 200)
                {
                    continue;
                }

                if (!TryParseRatio(row[valueColumn], out decimal evSales))
                {
                    continue;
                }

                // A duplicate bucket name means the file carries two rows for one industry; the first
                // wins so a re-parse of the same file is deterministic.
                if (seen.Add(bucket))
                {
                    parsed.Add(new DamodaranBucketObservation(bucket, evSales));
                }
            }

            if (parsed.Count == 0)
            {
                return Result.Failure<List<DamodaranBucketObservation>>(
                    ValuationBenchmarkErrors.DamodaranEmptyDataset);
            }

            return parsed;
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

        /// <summary>Lower-cased with whitespace and footnote digits removed, so "EV/ Sales1" matches "ev/sales".</summary>
        private static string Normalize(string header)
        {
            var builder = new StringBuilder(header.Length);
            foreach (char c in header)
            {
                // Non-printing characters (BOM, zero-width joiners, control codes) survive a copy-paste
                // through a spreadsheet and are invisible in the file, so they are dropped rather than
                // allowed to defeat the match.
                if (char.IsWhiteSpace(c) || char.IsDigit(c) || char.IsControl(c)
                    || c is ByteOrderMark or ZeroWidthSpace)
                {
                    continue;
                }

                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }

        private static bool TryParseRatio(string cell, out decimal value)
        {
            // Thousands separators, a percent sign or a stray currency mark can ride along; the ratio
            // itself is a plain decimal.
            string cleaned = cell.Replace(",", string.Empty).Replace("%", string.Empty).Replace("$", string.Empty).Trim();

            // Damodaran writes a missing cell as "NA" and an unusable one as "#DIV/0!" — both are skips,
            // not failures: the file is fine, that one industry has no ratio this year.
            if (cleaned.Length == 0
                || !decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                value = 0m;
                return false;
            }

            // A non-positive EV/Sales is not a usable multiple; negatives occur for loss-heavy buckets.
            return value > 0m;
        }

        /// <summary>
        /// The "found" half of the error message: the first few non-empty rows, so the admin can see
        /// what the file actually looks like instead of guessing why it was rejected.
        /// </summary>
        private static string DescribeHeaders(List<string[]> rows)
        {
            List<string> preview = rows
                .Take(HeaderSearchLimit)
                .Where(r => r.Any(c => c.Trim().Length > 0))
                .Take(3)
                .Select(r => string.Join(", ", r.Take(12).Select(c => c.Trim()).Where(c => c.Length > 0)))
                .ToList();

            return preview.Count == 0 ? "(empty file)" : string.Join(" | ", preview);
        }

        /// <summary>
        /// Minimal RFC 4180 reader: quoted fields, doubled quotes inside them, CR/LF inside them. Small
        /// enough to keep the import free of a dependency, strict enough for the files it reads.
        /// </summary>
        private static List<string[]> ReadCsv(string content)
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

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        fields.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        fields.Add(field.ToString());
                        field.Clear();
                        rows.Add([.. fields]);
                        fields.Clear();
                        break;
                    default:
                        field.Append(c);
                        break;
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
