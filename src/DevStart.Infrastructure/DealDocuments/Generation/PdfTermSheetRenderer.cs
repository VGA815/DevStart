using DevStart.Application.DealDocuments.Generation;
using DevStart.Domain.InvestmentApplications;
using DevStart.Infrastructure.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DevStart.Infrastructure.DealDocuments.Generation
{
    /// <summary>
    /// Lays the term sheet out as a paginated PDF.
    /// <para>
    /// The wording follows the markdown templates section for section, because the two files are the
    /// same term sheet in two forms and a reader comparing them should find the same statements in
    /// the same order. What differs is what the medium can do: numbers read as Russian
    /// (<c>1 500 000,00 ₽</c>), pages are numbered, the cap table repeats its header when it breaks,
    /// and sections are held together rather than split wherever the text happened to run out.
    /// </para>
    /// </summary>
    internal sealed class PdfTermSheetRenderer : ITermSheetPdfRenderer
    {
        private const float BodySize = 9.5f;
        private const float LabelWidth = 175f;

        private static readonly Color Ink = Color.FromHex("#111111");
        private static readonly Color Muted = Color.FromHex("#5a5a5a");
        private static readonly Color Rule = Color.FromHex("#d0d4d9");
        private static readonly Color Accent = Color.FromHex("#1a6ed8");
        private static readonly Color NoteBackground = Color.FromHex("#f4f6f9");
        private static readonly Color HeadBackground = Color.FromHex("#eef1f5");

        public byte[] Render(TermSheetModel model)
        {
            Document document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginHorizontal(18, Unit.Millimetre);
                    page.MarginTop(14, Unit.Millimetre);
                    page.MarginBottom(14, Unit.Millimetre);
                    page.DefaultTextStyle(t => t
                        .FontFamily(PdfDocuments.SerifFamily)
                        .FontSize(BodySize)
                        .LineHeight(1.35f)
                        .FontColor(Ink));

                    page.Header().Element(c => ComposeHeader(c, model));
                    page.Content().Element(c => ComposeContent(c, model));
                    page.Footer().Element(c => ComposeFooter(c, model));
                });
            })
            .WithMetadata(PdfDocuments.Metadata($"Term Sheet — {InstrumentTitle(model.Instrument)}", model.GeneratedAt));

            return PdfDocuments.ToBytes(document);
        }

        private static void ComposeHeader(IContainer container, TermSheetModel model) =>
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Term Sheet — {InstrumentTitle(model.Instrument)}")
                        .FontFamily(PdfDocuments.SansFamily).FontSize(8).FontColor(Muted);
                    row.ConstantItem(220).AlignRight().Text(model.StartupName)
                        .FontFamily(PdfDocuments.SansFamily).FontSize(8).FontColor(Muted);
                });
                column.Item().PaddingTop(4).LineHorizontal(0.6f).LineColor(Rule);
            });

        private static void ComposeFooter(IContainer container, TermSheetModel model) =>
            container.Column(column =>
            {
                column.Item().PaddingBottom(4).LineHorizontal(0.6f).LineColor(Rule);
                column.Item().Row(row =>
                {
                    // The deal id is what ties a printed copy back to the record on the platform. The
                    // file's SHA-256 is stored with the document rather than printed here — a hash of
                    // the bytes cannot appear inside the bytes it describes; a reader verifies by
                    // hashing the file they hold.
                    row.RelativeItem().Text($"Сделка {model.DealId}")
                        .FontFamily(PdfDocuments.SansFamily).FontSize(7).FontColor(Muted);

                    row.ConstantItem(90).AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(t => t.FontFamily(PdfDocuments.SansFamily).FontSize(7).FontColor(Muted));
                        text.Span("стр. ");
                        text.CurrentPageNumber();
                        text.Span(" из ");
                        text.TotalPages();
                    });

                    row.RelativeItem().AlignRight().Text($"Сформирован {RuFormat.Timestamp(model.GeneratedAt)}")
                        .FontFamily(PdfDocuments.SansFamily).FontSize(7).FontColor(Muted);
                });
            });

        private static void ComposeContent(IContainer container, TermSheetModel model) =>
            container.PaddingTop(10).Column(column =>
            {
                column.Item().Element(c => TitleBlock(c, model));

                Section(column, "Investment", c => InvestmentSection(c, model));
                Section(column, InvestorShareHeading(model.Instrument), c => InvestorShareSection(c, model));

                // Held apart from the short sections: a long cap table has to be allowed to break, so
                // instead of keeping it whole the heading reserves enough room that it is never left
                // stranded at the foot of a page, and the table repeats its own header after a break.
                column.Item().PaddingTop(14).EnsureSpace(90).Column(c =>
                {
                    Heading(c, "Cap table after deal");
                    c.Item().PaddingTop(6).Element(x => CapTable(x, model.CapTable));
                });

                column.Item().PaddingTop(14).EnsureSpace(70).Column(c =>
                {
                    Heading(c, "Founders & vesting");
                    c.Item().PaddingTop(6).Element(x => FoundersSection(x, model));
                });

                Section(column, "Standard terms (not negotiable in this template)", StandardTermsSection);
                Section(column, "Platform score & indicative range (informational)", c => ScoreSection(c, model));
                Section(column, "Warnings", c => WarningsSection(c, model));
            });

        private static void TitleBlock(IContainer container, TermSheetModel model) =>
            container.Column(column =>
            {
                column.Item().Text($"Term Sheet — {InstrumentTitle(model.Instrument)}")
                    .FontFamily(PdfDocuments.SansFamily).FontSize(18).Bold().FontColor(Ink);
                column.Item().PaddingTop(6).PaddingBottom(8).LineHorizontal(1.5f).LineColor(Accent);

                LabelledValue(column, "Startup", model.StartupName);
                LabelledValue(column, "Stage", model.StartupStage);
                LabelledValue(column, "Deal id", model.DealId.ToString());
                LabelledValue(column, "Application id", model.ApplicationId.ToString());
                LabelledValue(column, "Generated at", RuFormat.Timestamp(model.GeneratedAt));
            });

        private static void InvestmentSection(IContainer container, TermSheetModel model) =>
            container.Column(column =>
            {
                switch (model.Instrument)
                {
                    case InvestmentInstrument.ConvertibleLoan:
                        LabelledValue(column, "Instrument", "Convertible Loan (regulated by Russian FZ-354/2021)");
                        LabelledValue(column, "Principal", RuFormat.Money(model.Amount));
                        LabelledValue(column, "Valuation cap", RuFormat.Money(model.ValuationCap));
                        LabelledValue(column, "Discount", RuFormat.FractionAsPercent(model.DiscountFraction));
                        LabelledValue(column, "Interest rate", InterestRate(model.InterestRateFraction));
                        LabelledValue(column, "Term", TermMonths(model.TermMonths));
                        break;

                    case InvestmentInstrument.PricedRound:
                        LabelledValue(column, "Instrument", "Priced equity round");
                        LabelledValue(column, "Investment amount", RuFormat.Money(model.Amount));
                        LabelledValue(column, "Pre-money valuation", RuFormat.Money(model.PreMoneyValuation));
                        break;

                    default:
                        LabelledValue(column, "Instrument", "SAFE (Simple Agreement for Future Equity)");
                        LabelledValue(column, "Amount", RuFormat.Money(model.Amount));
                        LabelledValue(column, "Valuation cap", RuFormat.Money(model.ValuationCap));
                        LabelledValue(column, "Discount", RuFormat.FractionAsPercent(model.DiscountFraction));
                        break;
                }

                LabelledValue(column, "Liquidation preference", RuFormat.Multiplier(model.LiquidationPreference));
                LabelledValue(column, "Pro-rata rights", RuFormat.YesNo(model.ProRataRights));

                column.Item().PaddingTop(8).Element(c => Note(c, InstrumentNote(model.Instrument)));
            });

        private static void InvestorShareSection(IContainer container, TermSheetModel model) =>
            container.Column(column =>
            {
                LabelledValue(column, "Investor share after deal", RuFormat.Percent(model.InvestorSharePct));
                LabelledValue(column, "Founders' combined share after deal", RuFormat.Percent(model.FoundersTotalAfterPct));
                column.Item().PaddingTop(8).Element(c => Note(c, InvestorShareNote(model.Instrument)));
            });

        private static void CapTable(IContainer container, IReadOnlyList<TermSheetCapTableRow> rows) =>
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3.4f);
                    columns.RelativeColumn(1.8f);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(1.6f);
                });

                // Repeated on every page the table spills onto. Without it the second half of a long
                // cap table is a grid of numbers with nothing saying which column is which.
                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Party", left: true);
                    HeaderCell(header.Cell(), "Type", left: true);
                    HeaderCell(header.Cell(), "Before, %");
                    HeaderCell(header.Cell(), "After, %");
                    HeaderCell(header.Cell(), "Vested, %");
                });

                foreach (TermSheetCapTableRow row in rows)
                {
                    BodyCell(table.Cell(), row.PartyName, left: true);
                    BodyCell(table.Cell(), row.PartyType, left: true);
                    BodyCell(table.Cell(), RuFormat.Number(row.SharePctBefore));
                    BodyCell(table.Cell(), RuFormat.Number(row.SharePctAfter));
                    BodyCell(table.Cell(), RuFormat.Number(row.VestedPctAfter));
                }
            });

        private static void FoundersSection(IContainer container, TermSheetModel model) =>
            container.Column(column =>
            {
                if (model.Founders.Count == 0)
                {
                    column.Item().Text("No founders on record.").Italic().FontColor(Muted);
                    return;
                }

                column.Spacing(4);
                foreach (TermSheetFounder founder in model.Founders)
                {
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(10).Text("•").FontColor(Muted);
                        row.RelativeItem().Text(text =>
                        {
                            text.Span($"{founder.Name} — ").Bold();
                            text.Span($"{RuFormat.Percent(founder.EquityPercentage)} equity; ");

                            if (founder.VestingStartDate is { } start
                                && founder.VestingMonths is { } months
                                && founder.VestedPercentage is { } vested)
                            {
                                text.Span(
                                    $"vesting over {months} months with a {founder.CliffMonths ?? 0}-month cliff from {RuFormat.Date(start)}; " +
                                    $"{RuFormat.Percent(vested)} vested as of {RuFormat.Date(model.AsOf)}.");
                            }
                            else
                            {
                                text.Span("standard vesting: 4 years with a 1-year cliff.");
                            }
                        });
                    });
                }
            });

        private static void StandardTermsSection(IContainer container) =>
            container.Column(column =>
            {
                LabelledValue(column, "Anti-dilution", "Broad-Based Weighted Average (BBWA)");
                LabelledValue(column, "Founder vesting", "per the Founders & vesting section above (default: 4 years with a 1-year cliff)");
                LabelledValue(column, "Pro-rata rights", "as marked above");
                LabelledValue(column, "Exclusivity", "30 days, legally binding");
                LabelledValue(column, "Confidentiality", "standard");
            });

        private static void ScoreSection(IContainer container, TermSheetModel model) =>
            container.Column(column =>
            {
                TermSheetScore score = model.Score;

                if (!score.Available)
                {
                    // Whether the score is showable at all was decided by the composer. Printing zeros
                    // here would be a fabrication that outlives the screen: on paper nobody can ask.
                    column.Item().Text(
                        "Расчёт скоринга по этому проекту не выполнен — данных недостаточно. " +
                        "Баллы и ориентир диапазона стоимости в этом документе не приводятся.")
                        .Italic().FontColor(Muted);
                }
                else
                {
                    LabelledValue(column, "Total score", score.Total is { } total
                        ? $"{RuFormat.Number(total)} / 100"
                        : $"{RuFormat.NoData} / 100");

                    LabelledValue(column, "Factors",
                        $"Team {RuFormat.Number(score.Team)} · Market {RuFormat.Number(score.Market)} · " +
                        $"Product {RuFormat.Number(score.Product)} · Traction {RuFormat.Number(score.Traction)} · " +
                        $"Competition {(score.Competition is { } c ? RuFormat.Number(c) : RuFormat.NoData)}");

                    LabelledValue(column, "Расчётный ориентир диапазона стоимости",
                        $"{RuFormat.Money(score.ValuationLow)} – {RuFormat.Money(score.ValuationHigh)}");

                    LabelledValue(column, "Methods used", string.Join(", ", score.MethodsUsed));
                    LabelledValue(column, "Methodology version", score.MethodologyVersion ?? RuFormat.NoData);
                    LabelledValue(column, "Calculated at", RuFormat.Timestamp(score.CalculatedAt));
                }

                column.Item().PaddingTop(8).Element(c => Note(c, Disclaimer));
            });

        private static void WarningsSection(IContainer container, TermSheetModel model) =>
            container.Column(column =>
            {
                if (model.Warnings.Count == 0)
                {
                    column.Item().Text("No warnings.").Italic().FontColor(Muted);
                    return;
                }

                column.Spacing(4);
                foreach (TermSheetWarning warning in model.Warnings)
                {
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(10).Text("•").FontColor(Muted);
                        row.RelativeItem().Text(text =>
                        {
                            text.Span($"{warning.Code} ").Bold();
                            text.Span($"({warning.Severity}): ").FontColor(Muted);
                            text.Span(warning.Message);
                        });
                    });
                }
            });

        // ---- layout primitives -------------------------------------------------------------

        /// <summary>
        /// A short section: heading, rule and body kept on one page. <c>PreventPageBreak</c> moves the
        /// whole block to the next page rather than splitting it, and falls back to splitting only if
        /// it cannot fit on a page at all — so it is safe even for a section that grows unexpectedly.
        /// </summary>
        private static void Section(ColumnDescriptor column, string title, Action<IContainer> body) =>
            column.Item().PaddingTop(14).PreventPageBreak().Column(c =>
            {
                Heading(c, title);
                c.Item().PaddingTop(6).Element(body);
            });

        private static void Heading(ColumnDescriptor column, string title)
        {
            column.Item().Text(title.ToUpperInvariant())
                .FontFamily(PdfDocuments.SansFamily).FontSize(7.5f).Bold()
                .LetterSpacing(0.12f).FontColor(Muted);
            column.Item().PaddingTop(3).LineHorizontal(0.6f).LineColor(Rule);
        }

        private static void LabelledValue(ColumnDescriptor column, string label, string value) =>
            column.Item().PaddingTop(2).Row(row =>
            {
                row.ConstantItem(LabelWidth).Text(label)
                    .FontFamily(PdfDocuments.SansFamily).FontSize(8.5f).FontColor(Muted);
                row.RelativeItem().Text(value);
            });

        private static void Note(IContainer container, string text) =>
            container
                .Background(NoteBackground)
                .BorderLeft(2)
                .BorderColor(Accent)
                .PaddingVertical(6)
                .PaddingLeft(8)
                .PaddingRight(8)
                .Text(text)
                .FontSize(8.5f)
                .Italic()
                .FontColor(Muted);

        private static void HeaderCell(IContainer cell, string text, bool left = false) =>
            Align(cell.Background(HeadBackground).BorderBottom(0.8f).BorderColor(Rule).PaddingVertical(5).PaddingHorizontal(5), left)
                .Text(text)
                .FontFamily(PdfDocuments.SansFamily).FontSize(7.5f).Bold().FontColor(Muted);

        private static void BodyCell(IContainer cell, string text, bool left = false) =>
            Align(cell.BorderBottom(0.4f).BorderColor(Rule).PaddingVertical(4).PaddingHorizontal(5), left)
                .Text(text)
                .FontSize(8.5f);

        private static IContainer Align(IContainer container, bool left) =>
            left ? container.AlignLeft() : container.AlignRight();

        // ---- per-instrument wording (mirrors the markdown templates) -----------------------

        private static string InstrumentTitle(InvestmentInstrument instrument) => instrument switch
        {
            InvestmentInstrument.ConvertibleLoan => "Convertible Loan",
            InvestmentInstrument.PricedRound => "Priced Round",
            _ => "SAFE",
        };

        private static string InvestorShareHeading(InvestmentInstrument instrument) =>
            instrument == InvestmentInstrument.ConvertibleLoan
                ? "Calculated investor share at conversion"
                : "Calculated investor share";

        private static string InstrumentNote(InvestmentInstrument instrument) => instrument switch
        {
            InvestmentInstrument.ConvertibleLoan =>
                "If no qualifying priced round closes before the term expires, the investor may demand repayment of "
                + "principal plus accrued interest. Notarisation is required for execution.",
            InvestmentInstrument.PricedRound =>
                "A priced round requires full transactional documents (SHA, charter amendments, subscription agreement). "
                + "This MVP deliverable contains only the share calculation and standard terms; full document pack is "
                + "generated separately by counsel.",
            _ =>
                "Conversion math: investor's effective price will be min(valuation_cap, next_round_price × (1 − discount)). "
                + "Settlement and shareholder rights are governed by the executed SAFE agreement.",
        };

        private static string InvestorShareNote(InvestmentInstrument instrument) => instrument switch
        {
            InvestmentInstrument.ConvertibleLoan =>
                "Calculated as (principal + accrued_interest) / valuation_cap. Actual share at conversion depends on the "
                + "next priced round price relative to the cap.",
            InvestmentInstrument.PricedRound =>
                "Calculated as amount / (pre_money_valuation + amount) (post-money basis). No conversion discount applies "
                + "to a priced round.",
            _ =>
                "Calculated assuming conversion at the stated cap. Actual conversion math depends on the next priced round.",
        };

        private const string Disclaimer =
            "Дисклеймер. Указанный диапазон носит информационно-аналитический характер, рассчитан алгоритмически по "
            + "данным платформы и не является отчётом об оценке в значении Федерального закона № 135-ФЗ «Об оценочной "
            + "деятельности в Российской Федерации», а также не является индивидуальной инвестиционной рекомендацией в "
            + "значении Федерального закона № 39-ФЗ «О рынке ценных бумаг». Итоговые условия сделки определяются "
            + "сторонами самостоятельно.";

        private static string InterestRate(decimal? fraction) =>
            fraction is null ? RuFormat.Dash : $"{RuFormat.FractionAsPercent(fraction)} p.a.";

        private static string TermMonths(int? months) =>
            months is null ? RuFormat.Dash : $"{RuFormat.Months(months)} months";
    }
}
