using DevStart.Application.Abstractions.Data;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Application.Scoring;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.Startups;
using System.Globalization;
using System.Text;

namespace DevStart.Infrastructure.DealDocuments.Generation
{
    internal sealed class TermSheetGenerator(IFileStorage fileStorage) : ITermSheetGenerator
    {
        public async Task<string> RenderAsync(
            InvestmentDeal deal,
            Startup startup,
            ScoreResult score,
            CapTableResult capTable,
            CancellationToken cancellationToken)
        {
            string slug = deal.Instrument switch
            {
                InvestmentInstrument.Safe => "safe",
                InvestmentInstrument.ConvertibleLoan => "convertible",
                InvestmentInstrument.PricedRound => "priced",
                _ => "safe"
            };

            string template;
            using (Stream stream = await fileStorage.DownloadAsync(
                       DealDocumentBuckets.TermSheetTemplateKey(slug),
                       DealDocumentBuckets.Templates,
                       cancellationToken))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                template = await reader.ReadToEndAsync(cancellationToken);
            }

            CultureInfo c = CultureInfo.InvariantCulture;
            Dictionary<string, string> map = new()
            {
                ["startup_name"] = startup.Name,
                ["startup_stage"] = startup.Stage.ToString(),
                ["deal_id"] = deal.Id.ToString(),
                ["application_id"] = deal.ApplicationId.ToString(),
                ["amount"] = deal.Amount.ToString("N2", c),
                ["valuation_cap"] = deal.ValuationCap?.ToString("N2", c) ?? "—",
                ["discount_pct"] = (deal.Discount * 100m)?.ToString("0.##", c) ?? "—",
                ["interest_rate_pct"] = (deal.InterestRate * 100m)?.ToString("0.##", c) ?? "—",
                ["term_months"] = deal.TermMonths?.ToString(c) ?? "—",
                ["pre_money_valuation"] = deal.PreMoneyValuation?.ToString("N2", c) ?? "—",
                ["liquidation_preference"] = deal.LiquidationPreference.ToString("0.#", c),
                ["pro_rata_rights"] = deal.ProRataRights ? "Yes" : "No",
                ["investor_share_pct"] = capTable.InvestorSharePct.ToString("0.##", c),
                ["founders_total_after_pct"] = capTable.FoundersTotalAfterPct.ToString("0.##", c),
                ["score_total"] = score.TotalScore.ToString("0.##", c),
                ["score_team"] = score.TeamScore.ToString("0.##", c),
                ["score_market"] = score.MarketScore.ToString("0.##", c),
                ["score_product"] = score.ProductScore.ToString("0.##", c),
                ["score_traction"] = score.TractionScore.ToString("0.##", c),
                ["score_competition"] = score.CompetitionScore.ToString("0.##", c),
                ["valuation_low"] = score.ValuationLow.ToString("N2", c),
                ["valuation_high"] = score.ValuationHigh.ToString("N2", c),
                ["methods_used"] = string.Join(", ", score.MethodsUsed),
                ["calculated_at"] = score.CalculatedAt.ToString("yyyy-MM-dd HH:mm 'UTC'", c),
                ["cap_table_md_table"] = BuildCapTableMarkdown(capTable),
                ["flags_section"] = BuildFlagsSection(capTable.Warnings),
                ["generated_at"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", c)
            };

            string output = template;
            foreach ((string key, string value) in map)
            {
                output = output.Replace($"{{{{{key}}}}}", value);
            }
            return output;
        }

        private static string BuildCapTableMarkdown(CapTableResult capTable)
        {
            var sb = new StringBuilder();
            sb.AppendLine("| Party | Type | Before, % | After, % |");
            sb.AppendLine("|---|---|---:|---:|");
            CultureInfo c = CultureInfo.InvariantCulture;
            foreach (CapTableEntry e in capTable.Entries)
            {
                sb.AppendLine($"| {e.PartyName} | {e.PartyType} | {e.SharePctBefore.ToString("0.##", c)} | {e.SharePctAfter.ToString("0.##", c)} |");
            }
            return sb.ToString();
        }

        private static string BuildFlagsSection(IReadOnlyList<Application.Abstractions.Validation.DealTermsFlag> warnings)
        {
            if (warnings.Count == 0)
            {
                return "_No warnings._";
            }
            var sb = new StringBuilder();
            foreach (Application.Abstractions.Validation.DealTermsFlag w in warnings)
            {
                sb.AppendLine($"- **{w.Code}** ({w.Severity}): {w.Message}");
            }
            return sb.ToString();
        }
    }
}
