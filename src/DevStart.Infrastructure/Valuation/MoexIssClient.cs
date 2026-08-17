using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DevStart.Infrastructure.Valuation
{
    /// <summary>
    /// Reads a market capitalisation from MOEX ISS. Free, no key, no auth — which is the whole reason
    /// the numerator of the multiple comes from here.
    ///
    /// ISS returns column-oriented blocks (<c>columns</c> + <c>data</c>), so every field is looked up by
    /// column name rather than by position: the column order is not part of any contract MOEX publishes.
    /// </summary>
    // Public because the job that consumes it must be public for Hangfire to resolve the expression.
    public sealed class MoexIssClient(HttpClient httpClient, IOptions<MoexOptions> options)
    {
        private readonly MoexOptions _options = options.Value;

        /// <summary>
        /// Market capitalisation in RUB, or <c>null</c> when ISS has no figure for this ticker today
        /// (illiquid session, wrong board, delisted). A <c>null</c> is a normal outcome, not an error —
        /// the caller records a miss and moves to the next issuer.
        /// </summary>
        public async Task<decimal?> GetMarketCapAsync(string ticker, CancellationToken cancellationToken)
        {
            string url = $"{_options.BaseUrl.TrimEnd('/')}/iss/engines/stock/markets/shares/boards/"
                + $"{_options.Board}/securities/{Uri.EscapeDataString(ticker)}.json?iss.meta=off";

            using HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            // Preferred: ISS computes the capitalisation itself.
            decimal? capitalisation = ReadCell(document, "marketdata", "ISSUECAPITALIZATION");
            if (capitalisation is > 0m)
            {
                return capitalisation;
            }

            // Fallback: shares outstanding × last traded price. Off-session there is no LAST, so the
            // previous close stands in — a quarter-granularity benchmark does not care about the
            // difference, and refusing to record anything would be worse.
            decimal? issueSize = ReadCell(document, "securities", "ISSUESIZE");
            decimal? price = ReadCell(document, "marketdata", "LAST")
                ?? ReadCell(document, "securities", "PREVPRICE");

            return issueSize is > 0m && price is > 0m ? issueSize * price : null;
        }

        private static decimal? ReadCell(JsonDocument document, string block, string column)
        {
            if (!document.RootElement.TryGetProperty(block, out JsonElement blockElement)
                || !blockElement.TryGetProperty("columns", out JsonElement columns)
                || !blockElement.TryGetProperty("data", out JsonElement data)
                || data.GetArrayLength() == 0)
            {
                return null;
            }

            int index = -1;
            for (int i = 0; i < columns.GetArrayLength(); i++)
            {
                if (string.Equals(columns[i].GetString(), column, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                return null;
            }

            JsonElement row = data[0];
            if (index >= row.GetArrayLength())
            {
                return null;
            }

            JsonElement cell = row[index];
            return cell.ValueKind switch
            {
                JsonValueKind.Number => cell.TryGetDecimal(out decimal number) ? number : null,
                JsonValueKind.String => decimal.TryParse(
                    cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed)
                        ? parsed
                        : null,
                _ => null,
            };
        }
    }
}
