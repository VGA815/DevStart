using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DevStart.Infrastructure.Valuation
{
    /// <summary>
    /// Reads statutory (РСБУ) annual revenue by INN from ГИР БО. Three hops, because that is what the
    /// service exposes: find the organisation by INN, list its filed reports, read line 2110 (выручка)
    /// of the most recent one.
    ///
    /// Every hop is tolerant of a missing field and returns <c>null</c> rather than throwing: for this
    /// pipeline "ГИР БО has nothing for this company" is an empty cell, not a failure. Field names are
    /// looked up rather than positions assumed, and the shapes are read defensively — the service
    /// publishes no stable contract.
    /// </summary>
    // Public for the same reason as MoexIssClient: its consumer is a Hangfire job.
    public sealed class GirBoClient(HttpClient httpClient, IOptions<GirBoOptions> options)
    {
        private readonly GirBoOptions _options = options.Value;

        /// <summary>Latest filed annual revenue in RUB together with its fiscal year, or <c>null</c>.</summary>
        public async Task<(decimal Revenue, int FiscalYear)?> GetLatestRevenueAsync(
            string inn, CancellationToken cancellationToken)
        {
            long? organisationId = await FindOrganisationIdAsync(inn, cancellationToken);
            if (organisationId is null)
            {
                return null;
            }

            (long ReportId, int Year)? report = await FindLatestReportAsync(organisationId.Value, cancellationToken);
            if (report is null)
            {
                return null;
            }

            decimal? revenue = await ReadRevenueAsync(report.Value.ReportId, cancellationToken);
            return revenue is > 0m ? (revenue.Value, report.Value.Year) : null;
        }

        private async Task<long?> FindOrganisationIdAsync(string inn, CancellationToken cancellationToken)
        {
            string url = $"{Root}/nbo/organizations/search?query={Uri.EscapeDataString(inn)}&page=0";
            using JsonDocument? document = await GetJsonAsync(url, cancellationToken);
            if (document is null)
            {
                return null;
            }

            // The endpoint has shipped both a bare array and a paged {content: []} envelope.
            JsonElement root = document.RootElement;
            JsonElement items = root.ValueKind == JsonValueKind.Array
                ? root
                : root.TryGetProperty("content", out JsonElement content) ? content : default;

            if (items.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (JsonElement item in items.EnumerateArray())
            {
                // Search is fuzzy: only an exact INN match is this company.
                if (item.TryGetProperty("inn", out JsonElement innElement)
                    && string.Equals(AsString(innElement), inn, StringComparison.Ordinal)
                    && item.TryGetProperty("id", out JsonElement idElement)
                    && TryGetInt64(idElement, out long id))
                {
                    return id;
                }
            }

            return null;
        }

        private async Task<(long ReportId, int Year)?> FindLatestReportAsync(
            long organisationId, CancellationToken cancellationToken)
        {
            string url = $"{Root}/nbo/organizations/{organisationId}/bfo/";
            using JsonDocument? document = await GetJsonAsync(url, cancellationToken);
            if (document is null || document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            (long ReportId, int Year)? latest = null;
            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out JsonElement idElement)
                    || !TryGetInt64(idElement, out long id))
                {
                    continue;
                }

                if (!item.TryGetProperty("period", out JsonElement periodElement)
                    || !int.TryParse(AsString(periodElement), NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
                {
                    continue;
                }

                if (latest is null || year > latest.Value.Year)
                {
                    latest = (id, year);
                }
            }

            return latest;
        }

        private async Task<decimal?> ReadRevenueAsync(long reportId, CancellationToken cancellationToken)
        {
            string url = $"{Root}/nbo/bfo/{reportId}";
            using JsonDocument? document = await GetJsonAsync(url, cancellationToken);
            if (document is null)
            {
                return null;
            }

            // Line 2110 of form 2 is выручка. "current" is the reporting year; "previous" is the
            // comparative column and must never be mistaken for it.
            return FindNumberByName(document.RootElement, "current2110", depth: 0);
        }

        private string Root => _options.BaseUrl.TrimEnd('/');

        private async Task<JsonDocument?> GetJsonAsync(string url, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Depth-limited search for a named numeric property. The report body nests the form sections
        /// differently across filing years, so hunting for the line by name survives a reshuffle that a
        /// hardcoded path would not.
        /// </summary>
        private static decimal? FindNumberByName(JsonElement element, string name, int depth)
        {
            if (depth > 6)
            {
                return null;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            decimal? direct = AsDecimal(property.Value);
                            if (direct is not null)
                            {
                                return direct;
                            }
                        }

                        decimal? nested = FindNumberByName(property.Value, name, depth + 1);
                        if (nested is not null)
                        {
                            return nested;
                        }
                    }

                    return null;

                case JsonValueKind.Array:
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        decimal? nested = FindNumberByName(item, name, depth + 1);
                        if (nested is not null)
                        {
                            return nested;
                        }
                    }

                    return null;

                default:
                    return null;
            }
        }

        private static decimal? AsDecimal(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out decimal number) ? number : null,
            JsonValueKind.String => decimal.TryParse(
                element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed)
                    ? parsed
                    : null,
            _ => null,
        };

        private static string? AsString(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            _ => null,
        };

        private static bool TryGetInt64(JsonElement element, out long value)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number when element.TryGetInt64(out value):
                    return true;
                case JsonValueKind.String
                    when long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value):
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }
    }
}
