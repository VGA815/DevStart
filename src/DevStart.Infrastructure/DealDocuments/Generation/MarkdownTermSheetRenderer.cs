using DevStart.Application.Abstractions.Data;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Domain.InvestmentApplications;
using System.Globalization;
using System.Text;

namespace DevStart.Infrastructure.DealDocuments.Generation
{
    /// <summary>
    /// Fills the instrument's markdown template from storage with values formatted from the model.
    /// <para>
    /// Formatting stays on <see cref="CultureInfo.InvariantCulture"/> deliberately. The markdown is
    /// the machine-and-editor-facing form of the term sheet, and its exact bytes are pinned by a
    /// golden test; the PDF, which is what a person carries away, formats for ru-RU instead.
    /// </para>
    /// </summary>
    internal sealed class MarkdownTermSheetRenderer(IFileStorage fileStorage) : ITermSheetMarkdownRenderer
    {
        private const string Na = "N/A";
        private const string Dash = "—";

        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        public async Task<string> RenderAsync(TermSheetModel model, CancellationToken cancellationToken)
        {
            string template = await LoadTemplateAsync(model.Instrument, cancellationToken);
            TermSheetScore score = model.Score;

            Dictionary<string, string> map = new()
            {
                ["startup_name"] = model.StartupName,
                ["startup_stage"] = model.StartupStage,
                ["deal_id"] = model.DealId.ToString(),
                ["application_id"] = model.ApplicationId.ToString(),
                ["amount"] = model.Amount.ToString("N2", Culture),
                ["valuation_cap"] = model.ValuationCap?.ToString("N2", Culture) ?? Dash,
                ["discount_pct"] = (model.DiscountFraction * 100m)?.ToString("0.##", Culture) ?? Dash,
                ["interest_rate_pct"] = (model.InterestRateFraction * 100m)?.ToString("0.##", Culture) ?? Dash,
                ["term_months"] = model.TermMonths?.ToString(Culture) ?? Dash,
                ["pre_money_valuation"] = model.PreMoneyValuation?.ToString("N2", Culture) ?? Dash,
                ["liquidation_preference"] = model.LiquidationPreference.ToString("0.#", Culture),
                ["pro_rata_rights"] = model.ProRataRights ? "Yes" : "No",
                ["investor_share_pct"] = model.InvestorSharePct.ToString("0.##", Culture),
                ["founders_total_after_pct"] = model.FoundersTotalAfterPct.ToString("0.##", Culture),
                ["score_total"] = score.Available ? score.Total?.ToString("0.##", Culture) ?? Na : Na,
                ["score_team"] = score.Available ? score.Team.ToString("0.##", Culture) : Na,
                ["score_market"] = score.Available ? score.Market.ToString("0.##", Culture) : Na,
                ["score_product"] = score.Available ? score.Product.ToString("0.##", Culture) : Na,
                ["score_traction"] = score.Available ? score.Traction.ToString("0.##", Culture) : Na,
                ["score_competition"] = score.Available ? score.Competition?.ToString("0.##", Culture) ?? Na : Na,
                ["valuation_low"] = score.Available ? score.ValuationLow.ToString("N2", Culture) : Na,
                ["valuation_high"] = score.Available ? score.ValuationHigh.ToString("N2", Culture) : Na,
                ["methods_used"] = score.Available ? string.Join(", ", score.MethodsUsed) : Na,
                ["methodology_version"] = score.Available ? score.MethodologyVersion ?? Na : Na,
                ["calculated_at"] = score.Available ? FormatTimestamp(score.CalculatedAt) : Na,
                ["cap_table_md_table"] = BuildCapTableMarkdown(model.CapTable),
                ["founders_breakdown"] = BuildFoundersBreakdown(model.Founders, model.AsOf),
                ["flags_section"] = BuildFlagsSection(model.Warnings),
                ["generated_at"] = FormatTimestamp(model.GeneratedAt)
            };

            string output = template;
            foreach ((string key, string value) in map)
            {
                output = output.Replace($"{{{{{key}}}}}", value);
            }
            return output;
        }

        private async Task<string> LoadTemplateAsync(
            InvestmentInstrument instrument,
            CancellationToken cancellationToken)
        {
            string slug = instrument switch
            {
                InvestmentInstrument.Safe => "safe",
                InvestmentInstrument.ConvertibleLoan => "convertible",
                InvestmentInstrument.PricedRound => "priced",
                _ => "safe"
            };

            using Stream stream = await fileStorage.DownloadAsync(
                DealDocumentBuckets.TermSheetTemplateKey(slug),
                DealDocumentBuckets.Templates,
                cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        private static string FormatTimestamp(DateTime value) =>
            value.ToString("yyyy-MM-dd HH:mm 'UTC'", Culture);

        private static string BuildCapTableMarkdown(IReadOnlyList<TermSheetCapTableRow> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("| Party | Type | Before, % | After, % | Vested, % |");
            sb.AppendLine("|---|---|---:|---:|---:|");
            foreach (TermSheetCapTableRow e in rows)
            {
                sb.AppendLine($"| {e.PartyName} | {e.PartyType} | {e.SharePctBefore.ToString("0.##", Culture)} | {e.SharePctAfter.ToString("0.##", Culture)} | {e.VestedPctAfter.ToString("0.##", Culture)} |");
            }
            return sb.ToString();
        }

        // Per-founder breakdown with each founder's pre-deal stake and vesting schedule. Founders
        // without an explicit schedule fall back to the platform's standard vesting boilerplate.
        private static string BuildFoundersBreakdown(IReadOnlyList<TermSheetFounder> founders, DateTime asOf)
        {
            if (founders.Count == 0)
            {
                return "_No founders on record._";
            }

            var sb = new StringBuilder();
            foreach (TermSheetFounder f in founders)
            {
                string equity = f.EquityPercentage.ToString("0.##", Culture);

                if (f.VestingStartDate is { } start && f.VestingMonths is { } months && f.VestedPercentage is { } vestedPct)
                {
                    string vested = vestedPct.ToString("0.##", Culture);
                    int cliff = f.CliffMonths ?? 0;
                    sb.AppendLine(
                        $"- **{f.Name}** — {equity}% equity; vesting over {months} months with a {cliff}-month cliff from {start.ToString("yyyy-MM-dd", Culture)}; {vested}% vested as of {asOf.ToString("yyyy-MM-dd", Culture)}.");
                }
                else
                {
                    sb.AppendLine(
                        $"- **{f.Name}** — {equity}% equity; standard vesting: 4 years with a 1-year cliff.");
                }
            }

            return sb.ToString();
        }

        private static string BuildFlagsSection(IReadOnlyList<TermSheetWarning> warnings)
        {
            if (warnings.Count == 0)
            {
                return "_No warnings._";
            }

            var sb = new StringBuilder();
            foreach (TermSheetWarning w in warnings)
            {
                sb.AppendLine($"- **{w.Code}** ({w.Severity}): {w.Message}");
            }
            return sb.ToString();
        }
    }
}
