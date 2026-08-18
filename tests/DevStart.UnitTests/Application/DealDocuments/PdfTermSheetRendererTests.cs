using DevStart.Application.DealDocuments.Generation;
using DevStart.Application.StartupEquity;
using DevStart.Application.StartupEquity.Vesting;
using DevStart.Domain.StartupEquity;
using DevStart.Infrastructure.DealDocuments.Generation;
using DevStart.UnitTests.TestSupport;
using Shouldly;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DevStart.UnitTests.Application.DealDocuments;

/// <summary>
/// The PDF is checked for the properties that must hold, not against a reference file. A golden
/// comparison of PDF bytes would go red the next time QuestPDF or its Skia backend changes anything
/// about its output — a failure that says nothing about our code. Determinism, structure, and the
/// layout edge cases are invariants that survive a library upgrade.
/// </summary>
public sealed class PdfTermSheetRendererTests
{
    private static readonly PdfTermSheetRenderer Renderer = new();

    private static TermSheetModel Compose(TermSheetSamples.Sample sample) =>
        new TermSheetComposer(new VestingCalculator(), new FixedDateTimeProvider { UtcNow = TermSheetSamples.AsOf })
            .Compose(sample.Deal, sample.Startup, sample.Score, sample.CapTable, sample.FoundingHolders, TermSheetSamples.AsOf);

    /// <summary>
    /// The real invariant. Left alone QuestPDF stamps the wall clock into the PDF metadata, so every
    /// run produces different bytes and the stored SHA-256 identifies nothing. This test is what
    /// keeps the hash a fingerprint of content.
    /// </summary>
    [Theory]
    [InlineData("safe")]
    [InlineData("convertible")]
    [InlineData("priced")]
    public void RenderingTheSameModelTwice_ProducesIdenticalBytes(string sampleName)
    {
        TermSheetModel model = Compose(TermSheetSamples.All.Single(s => s.Name == sampleName));

        byte[] first = Renderer.Render(model);
        byte[] second = Renderer.Render(model);

        first.ShouldBe(second);
    }

    [Fact]
    public void DifferentDeals_ProduceDifferentBytes()
    {
        byte[] safe = Renderer.Render(Compose(TermSheetSamples.Safe()));
        byte[] priced = Renderer.Render(Compose(TermSheetSamples.Priced()));

        safe.ShouldNotBe(priced);
    }

    [Fact]
    public void TheDocument_CarriesTheDealsKeyValues()
    {
        TermSheetModel model = Compose(TermSheetSamples.Safe());

        string text = ExtractText(Renderer.Render(model));

        text.ShouldContain("Кофейня на Луне");
        text.ShouldContain(model.DealId.ToString());
        // ru-RU formatting: non-breaking group separators, comma for the decimal point.
        text.ShouldContain("1 500 000,00 ₽");
        text.ShouldContain("60 000 000,00 ₽");
        text.ShouldContain("Term Sheet — SAFE");
    }

    [Fact]
    public void EveryCapTableRow_ReachesTheDocument()
    {
        TermSheetSamples.Sample sample = TermSheetSamples.Safe();
        TermSheetModel model = Compose(sample);

        string text = ExtractText(Renderer.Render(model));

        foreach (TermSheetCapTableRow row in model.CapTable)
        {
            text.ShouldContain(row.PartyName);
        }
    }

    /// <summary>
    /// The composer's "no data" answer has to survive onto paper as words. A zero printed here would
    /// be permanent in a way a zero on screen is not.
    /// </summary>
    [Fact]
    public void AnUnavailableScore_PrintsAsNoDataRatherThanZero()
    {
        TermSheetModel model = Compose(TermSheetSamples.Priced());

        string text = ExtractText(Renderer.Render(model));

        model.Score.Available.ShouldBeFalse();
        ShouldContainPhrase(text, "данных недостаточно");
        text.ShouldNotContain("0 / 100");
        text.ShouldNotContain("0,00 ₽ – 0,00 ₽");
    }

    [Fact]
    public void TheDisclaimer_IsOnTheDocument()
    {
        string text = ExtractText(Renderer.Render(Compose(TermSheetSamples.Safe())));

        ShouldContainPhrase(text, "не является отчётом об оценке");
        ShouldContainPhrase(text, "Расчётный ориентир диапазона стоимости");
    }

    [Fact]
    public void EveryPage_IsNumbered()
    {
        byte[] pdf = Renderer.Render(Compose(TermSheetSamples.Safe()));

        using PdfDocument document = PdfDocument.Open(pdf);
        document.NumberOfPages.ShouldBeGreaterThan(0);

        for (int i = 1; i <= document.NumberOfPages; i++)
        {
            document.GetPage(i).Text.ShouldContain($"стр. {i} из {document.NumberOfPages}");
        }
    }

    // ---- layout edge cases -----------------------------------------------------------------
    // QuestPDF retries an unresolvable layout a few times and then throws. These are the shapes that
    // provoke it, caught here rather than in a Hangfire worker at three in the morning.

    [Fact]
    public void AVeryLongStartupName_StillLaysOut()
    {
        TermSheetSamples.Sample sample = TermSheetSamples.Safe();
        sample.Startup.Name = string.Join(" ", Enumerable.Repeat("Научно-производственное объединение", 12));

        byte[] pdf = Renderer.Render(Compose(sample));

        ExtractText(pdf).ShouldContain("Научно-производственное объединение");
    }

    [Fact]
    public void ACapTableOfFortyRows_BreaksAcrossPagesAndRepeatsItsHeader()
    {
        TermSheetSamples.Sample sample = TermSheetSamples.Safe();
        var rows = Enumerable.Range(1, 40)
            .Select(i => new CapTableEntry(Guid.NewGuid(), $"Участник {i}", "Advisor", 1m, 0.875m, 0.875m))
            .ToList();

        TermSheetModel model = Compose(sample) with { CapTable = [.. rows.Select(r =>
            new TermSheetCapTableRow(r.PartyName, r.PartyType, r.SharePctBefore, r.SharePctAfter, r.VestedPctAfter))] };

        byte[] pdf = Renderer.Render(model);

        using PdfDocument document = PdfDocument.Open(pdf);
        document.NumberOfPages.ShouldBeGreaterThan(1);

        // The header must appear on every page the table actually spills onto, or the continuation is
        // a grid of unlabelled numbers.
        List<Page> pagesWithRows = [.. Enumerable.Range(1, document.NumberOfPages)
            .Select(document.GetPage)
            .Where(p => p.Text.Contains("Участник "))];

        pagesWithRows.Count.ShouldBeGreaterThan(1);
        pagesWithRows.ShouldAllBe(p => p.Text.Contains("Before, %"));
    }

    [Fact]
    public void ADealWithNoOptionalTerms_StillLaysOut()
    {
        TermSheetSamples.Sample sample = TermSheetSamples.Safe();
        sample.Deal.ValuationCap = null;
        sample.Deal.Discount = null;
        sample.Deal.InterestRate = null;
        sample.Deal.TermMonths = null;
        sample.Deal.PreMoneyValuation = null;

        TermSheetModel model = Compose(sample) with { Founders = [], Warnings = [] };

        string text = ExtractText(Renderer.Render(model));

        text.ShouldContain("No founders on record.");
        text.ShouldContain("No warnings.");
        text.ShouldContain("—");
    }

    [Fact]
    public void AFounderListLongEnoughToPaginate_StillLaysOut()
    {
        TermSheetSamples.Sample sample = TermSheetSamples.Safe();
        var founders = Enumerable.Range(1, 60)
            .Select(i => new FoundingCapTableHolder(
                Guid.NewGuid(), EquityHolderType.Founder, $"Основатель номер {i}", 1.5m,
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), 48, 12))
            .ToList();

        TermSheetModel model = new TermSheetComposer(
                new VestingCalculator(), new FixedDateTimeProvider { UtcNow = TermSheetSamples.AsOf })
            .Compose(sample.Deal, sample.Startup, sample.Score, sample.CapTable, founders, TermSheetSamples.AsOf);

        string text = ExtractText(Renderer.Render(model));

        text.ShouldContain("Основатель номер 1 ");
        text.ShouldContain("Основатель номер 60 ");
    }

    /// <summary>
    /// Nothing may run off the sheet. Text that overflows the page is still "successfully rendered"
    /// as far as the renderer is concerned, and the only way to notice is to look at where the glyphs
    /// actually landed.
    /// </summary>
    [Theory]
    [InlineData("safe")]
    [InlineData("convertible")]
    [InlineData("priced")]
    public void NoText_FallsOutsideThePage(string sampleName)
    {
        byte[] pdf = Renderer.Render(Compose(TermSheetSamples.All.Single(s => s.Name == sampleName)));

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
    /// no space at a wrap, so "отчётом об оценке" can come back as "отчётом обоценке"; comparing with
    /// all whitespace removed checks the wording without pinning the layout.
    /// </summary>
    private static void ShouldContainPhrase(string text, string phrase) =>
        Squash(text).ShouldContain(Squash(phrase));

    private static string Squash(string value) =>
        string.Concat(value.Where(c => !char.IsWhiteSpace(c) && c != '\u00a0'));
}
