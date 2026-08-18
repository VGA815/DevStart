using DevStart.Application.ScoringReports;
using DevStart.Infrastructure.ScoringReports;
using DevStart.UnitTests.TestSupport;
using Shouldly;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DevStart.UnitTests.Application.ScoringReports;

/// <summary>
/// The report goes out of the building on paper, so what it must not lose in translation is asserted
/// here: the methodology version, where each factor's points came from, and "no data" said as no
/// data. Plus the same determinism invariant the term-sheet renderer holds.
/// </summary>
public sealed class PdfScoringReportRendererTests
{
    private static readonly PdfScoringReportRenderer Renderer = new();

    private static ScoringReportModel Compose(bool available = true) =>
        new ScoringReportComposer(new FixedDateTimeProvider { UtcNow = ScoringReportSamples.Now })
            .Compose(
                ScoringReportSamples.Startup(),
                available ? ScoringReportSamples.Score() : ScoringReportSamples.NoData());

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RenderingTheSameModelTwice_ProducesIdenticalBytes(bool available)
    {
        ScoringReportModel model = Compose(available);

        Renderer.Render(model).ShouldBe(Renderer.Render(model));
    }

    /// <summary>
    /// Without it a report cannot be set beside a later one — the same reason the version rides on
    /// every valuation snapshot.
    /// </summary>
    [Fact]
    public void TheMethodologyVersion_IsOnEveryPage()
    {
        byte[] pdf = Renderer.Render(Compose());

        using PdfDocument document = PdfDocument.Open(pdf);
        foreach (Page page in document.GetPages())
        {
            page.Text.ShouldContain("Методика 2026.05");
        }
    }

    /// <summary>
    /// Half of what the report is worth to an investor is knowing which points rest on the startup's
    /// own word and which on something it cannot edit.
    /// </summary>
    [Fact]
    public void EachFactor_CarriesWhereItsPointsCameFrom()
    {
        string text = ExtractText(Renderer.Render(Compose()));

        ShouldContainPhrase(text, "самодекларация проекта");
        text.ShouldContain("внешний бенчмарк");
        text.ShouldContain("данные платформы");
        // Named for what the platform did, and explicitly scoreless — the register says who the
        // rightsholder is, not that this startup controls the patent.
        ShouldContainPhrase(text, "сверено с реестром (без баллов)");
    }

    [Fact]
    public void EveryFactor_IsNamedInRussian()
    {
        string text = ExtractText(Renderer.Render(Compose()));

        foreach (string label in new[] { "Команда", "Рынок", "Продукт", "Трекшн", "Конкуренция" })
        {
            text.ShouldContain(label);
        }
    }

    /// <summary>
    /// A factor that dropped out is a gap in the inputs, not a zero score. On screen a dash can be
    /// questioned; in a PDF it is permanent, so the document has to explain itself.
    /// </summary>
    [Fact]
    public void ADroppedFactor_PrintsADashAndExplainsTheRedistributedWeight()
    {
        ScoringReportModel model = Compose();
        string text = ExtractText(Renderer.Render(model));

        model.Factors.Single(f => f.Factor == "Competition").Participated.ShouldBeFalse();
        ShouldContainPhrase(text, "вес перераспределён");
        ShouldContainPhrase(text, "не участвовал в расчёте");
    }

    [Fact]
    public void AnEntirelyUnavailableScore_SaysSoInsteadOfPrintingZeros()
    {
        ScoringReportModel model = Compose(available: false);
        string text = ExtractText(Renderer.Render(model));

        model.Available.ShouldBeFalse();
        ShouldContainPhrase(text, "данных недостаточно");
        text.ShouldNotContain("0 из 100");
        text.ShouldNotContain("0,00 ₽");
    }

    /// <summary>
    /// The platform says "расчётный ориентир диапазона стоимости", never "оценка" — the wording was
    /// settled for ФЗ-135 reasons, and a document a reader shows to third parties is the last place
    /// to drift from it.
    /// </summary>
    [Fact]
    public void TheWording_StaysOffTheWordValuation()
    {
        string text = ExtractText(Renderer.Render(Compose()));

        text.ShouldContain("РАСЧЁТНЫЙ ОРИЕНТИР ДИАПАЗОНА СТОИМОСТИ");
        ShouldContainPhrase(text, "не является отчётом об оценке");
        ShouldContainPhrase(text, "не является индивидуальной инвестиционной рекомендацией");
    }

    [Fact]
    public void TheDisclaimer_IsPresentEvenWhenThereIsNoScore()
    {
        string text = ExtractText(Renderer.Render(Compose(available: false)));

        ShouldContainPhrase(text, "не является отчётом об оценке");
    }

    [Fact]
    public void TheValuationRange_IsFormattedForARussianReader()
    {
        string text = ExtractText(Renderer.Render(Compose()));

        text.ShouldContain("48 000 000,00 ₽");
        text.ShouldContain("72 000 000,00 ₽");
    }

    [Fact]
    public void EveryPage_IsNumbered()
    {
        byte[] pdf = Renderer.Render(Compose());

        using PdfDocument document = PdfDocument.Open(pdf);
        for (int i = 1; i <= document.NumberOfPages; i++)
        {
            document.GetPage(i).Text.ShouldContain($"стр. {i} из {document.NumberOfPages}");
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoText_FallsOutsideThePage(bool available)
    {
        byte[] pdf = Renderer.Render(Compose(available));

        using PdfDocument document = PdfDocument.Open(pdf);
        foreach (Page page in document.GetPages())
        {
            foreach (Word word in page.GetWords())
            {
                word.BoundingBox.Left.ShouldBeGreaterThanOrEqualTo(0);
                word.BoundingBox.Bottom.ShouldBeGreaterThanOrEqualTo(0);
                word.BoundingBox.Right.ShouldBeLessThanOrEqualTo(page.Width + 0.5);
                word.BoundingBox.Top.ShouldBeLessThanOrEqualTo(page.Height + 0.5);
            }
        }
    }

    [Fact]
    public void AVeryLongStartupName_StillLaysOut()
    {
        ScoringReportModel model = Compose() with
        {
            StartupName = string.Join(" ", Enumerable.Repeat("Научно-производственное объединение", 12)),
        };

        ExtractText(Renderer.Render(model)).ShouldContain("Научно-производственное объединение");
    }

    /// <summary>
    /// Extracted text, with the ru-RU non-breaking group separator folded to a plain space so
    /// assertions can spell amounts the way a person would rather than hiding a U+00A0 in a literal.
    /// </summary>
    private static string ExtractText(byte[] pdf)
    {
        using PdfDocument document = PdfDocument.Open(pdf);
        return string.Join("\n", document.GetPages().Select(p => p.Text)).Replace('\u00a0', ' ');
    }

    /// <summary>
    /// Asserts a phrase is present regardless of where the line broke. PDF text extraction inserts
    /// no space at a wrap, so "отчётом об оценке" comes back as "отчётом обоценке"; comparing with all
    /// whitespace removed checks the wording without pinning the layout.
    /// </summary>
    private static void ShouldContainPhrase(string text, string phrase) =>
        Squash(text).ShouldContain(Squash(phrase));

    private static string Squash(string value) =>
        string.Concat(value.Where(c => !char.IsWhiteSpace(c) && c != '\u00a0'));
}
