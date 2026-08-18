using DevStart.Application.Scoring;
using DevStart.Application.ScoringReports;
using DevStart.Infrastructure.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DevStart.Infrastructure.ScoringReports
{
    /// <summary>
    /// Lays out the scoring report. Shares the fonts, the fixed metadata and the ru-RU formatting
    /// with the term-sheet renderer, so both documents are recognisably from the same platform and
    /// both are byte-reproducible.
    /// <para>
    /// Three things this document must not lose on its way from screen to paper: the methodology
    /// version, without which it cannot be set beside a later report; where each factor's points came
    /// from, which is most of what the number is worth to a reader; and "no data" stated as no data
    /// rather than as zero.
    /// </para>
    /// </summary>
    internal sealed class PdfScoringReportRenderer : IScoringReportPdfRenderer
    {
        private const float BodySize = 9.5f;
        private const float LabelWidth = 190f;

        private static readonly Color Ink = Color.FromHex("#111111");
        private static readonly Color Muted = Color.FromHex("#5a5a5a");
        private static readonly Color Rule = Color.FromHex("#d0d4d9");
        private static readonly Color Accent = Color.FromHex("#1a6ed8");
        private static readonly Color NoteBackground = Color.FromHex("#f4f6f9");
        private static readonly Color HeadBackground = Color.FromHex("#eef1f5");

        public byte[] Render(ScoringReportModel model)
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
            .WithMetadata(PdfDocuments.Metadata($"Скоринг-отчёт — {model.StartupName}", model.GeneratedAt));

            return PdfDocuments.ToBytes(document);
        }

        private static void ComposeHeader(IContainer container, ScoringReportModel model) =>
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Скоринг-отчёт")
                        .FontFamily(PdfDocuments.SansFamily).FontSize(8).FontColor(Muted);
                    row.ConstantItem(240).AlignRight().Text(model.StartupName)
                        .FontFamily(PdfDocuments.SansFamily).FontSize(8).FontColor(Muted);
                });
                column.Item().PaddingTop(4).LineHorizontal(0.6f).LineColor(Rule);
            });

        private static void ComposeFooter(IContainer container, ScoringReportModel model) =>
            container.Column(column =>
            {
                column.Item().PaddingBottom(4).LineHorizontal(0.6f).LineColor(Rule);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Проект {model.StartupId}")
                        .FontFamily(PdfDocuments.SansFamily).FontSize(7).FontColor(Muted);

                    row.ConstantItem(90).AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(t => t.FontFamily(PdfDocuments.SansFamily).FontSize(7).FontColor(Muted));
                        text.Span("стр. ");
                        text.CurrentPageNumber();
                        text.Span(" из ");
                        text.TotalPages();
                    });

                    // The methodology version rides in the footer as well as in the body: it is what
                    // makes two reports comparable, and a page torn out of context should still carry it.
                    row.RelativeItem().AlignRight().Text(
                        $"Методика {model.MethodologyVersion ?? RuFormat.NoData} · {RuFormat.Timestamp(model.GeneratedAt)}")
                        .FontFamily(PdfDocuments.SansFamily).FontSize(7).FontColor(Muted);
                });
            });

        private static void ComposeContent(IContainer container, ScoringReportModel model) =>
            container.PaddingTop(10).Column(column =>
            {
                column.Item().Element(c => TitleBlock(c, model));

                if (!model.Available)
                {
                    Section(column, "Результат", c => c.Column(inner =>
                    {
                        inner.Item().Text(
                            "Расчёт по этому проекту не выполнен: данных недостаточно ни по одному фактору. " +
                            "Баллы и расчётный ориентир диапазона стоимости в этом отчёте не приводятся — " +
                            "нулевые значения были бы не результатом, а его имитацией.")
                            .Italic().FontColor(Muted);
                    }));

                    Section(column, "О характере расчёта", c => Note(c, Disclaimer));
                    return;
                }

                Section(column, "Результат", c => ResultSection(c, model));

                column.Item().PaddingTop(14).EnsureSpace(90).Column(c =>
                {
                    Heading(c, "Факторы");
                    c.Item().PaddingTop(6).Element(x => FactorsTable(x, model.Factors));
                    c.Item().PaddingTop(6).Element(FactorsLegend);
                });

                Section(column, "Расчётный ориентир диапазона стоимости", c => ValuationSection(c, model));
                Section(column, "О характере расчёта", c => Note(c, Disclaimer));
            });

        private static void TitleBlock(IContainer container, ScoringReportModel model) =>
            container.Column(column =>
            {
                column.Item().Text("Скоринг-отчёт")
                    .FontFamily(PdfDocuments.SansFamily).FontSize(18).Bold().FontColor(Ink);
                column.Item().PaddingTop(6).PaddingBottom(8).LineHorizontal(1.5f).LineColor(Accent);

                LabelledValue(column, "Проект", model.StartupName);
                LabelledValue(column, "Стадия", model.StartupStage);
                LabelledValue(column, "Идентификатор проекта", model.StartupId.ToString());
                LabelledValue(column, "Версия методики", model.MethodologyVersion ?? RuFormat.NoData);
                LabelledValue(column, "Расчёт выполнен", RuFormat.Timestamp(model.CalculatedAt));
                LabelledValue(column, "Отчёт сформирован", RuFormat.Timestamp(model.GeneratedAt));
            });

        private static void ResultSection(IContainer container, ScoringReportModel model) =>
            container.Column(column =>
            {
                LabelledValue(column, "Итоговый балл", model.TotalScore is { } total
                    ? $"{RuFormat.Number(total)} из 100"
                    : "недостаточно данных");

                LabelledValue(column, "Методы расчёта", string.Join(", ", model.MethodsUsed));

                int dropped = model.Factors.Count(f => !f.Participated);
                if (dropped > 0)
                {
                    column.Item().PaddingTop(8).Element(c => Note(c,
                        $"По {DroppedFactorsPhrase(dropped)} данных нет. " +
                        "Такой фактор не участвует в расчёте, а его вес перераспределён между остальными: " +
                        "итоговый балл считается по тому, что известно, и не занижается за отсутствие данных."));
                }
            });

        private static void FactorsTable(IContainer container, IReadOnlyList<ScoringReportFactor> factors) =>
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.2f);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(4f);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Фактор", left: true);
                    HeaderCell(header.Cell(), "Балл");
                    HeaderCell(header.Cell(), "Вес");
                    HeaderCell(header.Cell(), "Источник данных", left: true);
                });

                foreach (ScoringReportFactor factor in factors)
                {
                    BodyCell(table.Cell(), FactorLabel(factor.Factor), left: true);

                    // An em dash, never a zero: the factor did not score badly, it did not take part.
                    BodyCell(table.Cell(), factor.Score is { } score ? RuFormat.Number(score) : RuFormat.Dash);
                    BodyCell(table.Cell(), factor.Participated ? RuFormat.Percent(factor.Weight * 100m) : RuFormat.Dash);
                    BodyCell(table.Cell(), SourceLabel(factor.Source), left: true);
                }
            });

        private static void FactorsLegend(IContainer container) =>
            container.Text(
                "Прочерк в баллах означает отсутствие данных по фактору, а не нулевую оценку: такой фактор "
                + "не участвовал в расчёте, его вес перераспределён. Источник данных показывает, на чём держатся "
                + "баллы — на том, что заявил проект, или на том, что он изменить не может.")
                .FontSize(8).Italic().FontColor(Muted);

        private static void ValuationSection(IContainer container, ScoringReportModel model) =>
            container.Column(column =>
            {
                LabelledValue(column, "Нижняя граница", RuFormat.Money(model.ValuationLow));
                LabelledValue(column, "Верхняя граница", RuFormat.Money(model.ValuationHigh));
                if (model.ValuationPoint > 0m)
                {
                    LabelledValue(column, "Расчётная середина", RuFormat.Money(model.ValuationPoint));
                }
                LabelledValue(column, "Методы расчёта", string.Join(", ", model.MethodsUsed));
            });

        // ---- layout primitives (mirrors the term-sheet renderer) ---------------------------

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

        // ---- vocabulary --------------------------------------------------------------------

        /// <summary>
        /// Factor names are a closed set fixed by the scoring engine, so translating them here is safe.
        /// The per-component code vocabulary is a different matter and deliberately stays on the
        /// client: it grows with every methodology change, and a second copy would drift.
        /// An unknown factor prints its own name rather than a guess.
        /// </summary>
        private static string FactorLabel(string factor) => factor switch
        {
            "Team" => "Команда",
            "Market" => "Рынок",
            "Product" => "Продукт",
            "Traction" => "Трекшн",
            "Competition" => "Конкуренция",
            _ => factor,
        };

        /// <summary>
        /// Where the points came from. Named for what the platform actually did — in particular
        /// "сверено с реестром" says a claimed patent number resolves in the local copy of the
        /// register under the declared ИНН, which is not the same as ownership being verified, and it
        /// carries no points.
        /// </summary>
        private static string SourceLabel(ScoreFactorSource source)
        {
            if (source == ScoreFactorSource.None)
            {
                return "нет данных";
            }

            List<string> parts = [];
            if (source.HasFlag(ScoreFactorSource.SelfReported))
            {
                parts.Add("самодекларация проекта");
            }
            if (source.HasFlag(ScoreFactorSource.PlatformDerived))
            {
                parts.Add("данные платформы");
            }
            if (source.HasFlag(ScoreFactorSource.ExternalBenchmark))
            {
                parts.Add("внешний бенчмарк");
            }
            if (source.HasFlag(ScoreFactorSource.RegistryChecked))
            {
                parts.Add("сверено с реестром (без баллов)");
            }

            return string.Join("; ", parts);
        }

        private static string DroppedFactorsPhrase(int count) =>
            count == 1 ? "одному фактору" : $"{count} факторам";

        private const string Disclaimer =
            "Настоящий документ носит информационно-аналитический характер. Приведённые баллы и границы "
            + "рассчитаны алгоритмически по данным платформы и по данным, заявленным самим проектом. "
            + "Документ не является отчётом об оценке в значении Федерального закона № 135-ФЗ «Об оценочной "
            + "деятельности в Российской Федерации», не является индивидуальной инвестиционной рекомендацией "
            + "в значении Федерального закона № 39-ФЗ «О рынке ценных бумаг» и не может служить основанием "
            + "для определения цены сделки. Решения стороны принимают самостоятельно.";
    }
}
