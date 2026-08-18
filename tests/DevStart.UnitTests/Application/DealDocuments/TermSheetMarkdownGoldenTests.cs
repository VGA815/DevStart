using DevStart.Application.DealDocuments.Generation;
using DevStart.Application.StartupEquity.Vesting;
using DevStart.Infrastructure.DealDocuments.Generation;
using DevStart.UnitTests.TestSupport;
using Shouldly;
using System.Reflection;
using System.Text;

namespace DevStart.UnitTests.Application.DealDocuments;

/// <summary>
/// Pins the markdown term sheet against fixtures captured from the generator as it was before the
/// composer/renderer split. Placeholder substitution carries dozens of small formatting decisions —
/// which values get thousands separators, which get an em dash when absent, how a founder without a
/// vesting schedule is worded — and any of them is easy to lose while moving code. Without this the
/// refactor would be a matter of hope.
/// <para>
/// Comparison is exact except for line endings: the renderer builds its table and list sections with
/// <c>StringBuilder.AppendLine</c>, which emits <c>Environment.NewLine</c>, and the repository
/// normalizes text files on checkout. Normalizing both sides keeps the fixtures portable while still
/// pinning every character that carries meaning.
/// </para>
/// </summary>
public sealed class TermSheetMarkdownGoldenTests
{
    private static MarkdownTermSheetRenderer CreateRenderer() => new(new TemplateFileStorage());

    private static TermSheetComposer CreateComposer() =>
        new(new VestingCalculator(), new FixedDateTimeProvider { UtcNow = TermSheetSamples.AsOf });

    [Theory]
    [InlineData("safe")]
    [InlineData("convertible")]
    [InlineData("priced")]
    public async Task Rendering_MatchesTheCapturedOutput(string sampleName)
    {
        TermSheetSamples.Sample sample = TermSheetSamples.All.Single(s => s.Name == sampleName);

        TermSheetModel model = CreateComposer().Compose(
            sample.Deal, sample.Startup, sample.Score, sample.CapTable, sample.FoundingHolders, TermSheetSamples.AsOf);

        string actual = await CreateRenderer().RenderAsync(model, CancellationToken.None);

        Normalize(actual).ShouldBe(Normalize(ReadFixture(sampleName)));
    }

    /// <summary>
    /// The N/A rule is the composer's, not the renderer's — a renderer that decided for itself would
    /// eventually decide differently from its sibling. Asserted on the model so the PDF renderer
    /// inherits the same answer without a second copy of the rule.
    /// </summary>
    [Fact]
    public void ScoringWithoutMethods_ComposesAsUnavailable()
    {
        TermSheetSamples.Sample sample = TermSheetSamples.Priced();

        TermSheetModel model = CreateComposer().Compose(
            sample.Deal, sample.Startup, sample.Score, sample.CapTable, sample.FoundingHolders, TermSheetSamples.AsOf);

        model.Score.Available.ShouldBeFalse();
        model.Score.Total.ShouldBeNull();
        model.Score.MethodsUsed.ShouldBeEmpty();
    }

    [Fact]
    public void ScoringWithMethods_ComposesAsAvailable()
    {
        TermSheetSamples.Sample sample = TermSheetSamples.Safe();

        TermSheetModel model = CreateComposer().Compose(
            sample.Deal, sample.Startup, sample.Score, sample.CapTable, sample.FoundingHolders, TermSheetSamples.AsOf);

        model.Score.Available.ShouldBeTrue();
        model.Score.Total.ShouldBe(72.4567m);
        model.Score.MethodologyVersion.ShouldBe("2026.05");
    }

    /// <summary>An empty methodology version is missing data, and the model says so with null.</summary>
    [Fact]
    public void EmptyMethodologyVersion_ComposesAsNull()
    {
        TermSheetSamples.Sample sample = TermSheetSamples.Convertible();

        TermSheetModel model = CreateComposer().Compose(
            sample.Deal, sample.Startup, sample.Score, sample.CapTable, sample.FoundingHolders, TermSheetSamples.AsOf);

        model.Score.Available.ShouldBeTrue();
        model.Score.MethodologyVersion.ShouldBeNull();
    }

    /// <summary>
    /// A founder on the platform's standard vesting has no computed vested amount — the model carries
    /// null rather than a zero that a renderer could mistake for "nothing has vested".
    /// </summary>
    [Fact]
    public void FounderWithoutASchedule_HasNoVestedAmount()
    {
        TermSheetSamples.Sample sample = TermSheetSamples.Safe();

        TermSheetModel model = CreateComposer().Compose(
            sample.Deal, sample.Startup, sample.Score, sample.CapTable, sample.FoundingHolders, TermSheetSamples.AsOf);

        // Non-founder holders (the ESOP pool) never reach the founders section.
        model.Founders.Count.ShouldBe(2);

        TermSheetFounder scheduled = model.Founders[0];
        scheduled.VestingMonths.ShouldBe(48);
        scheduled.VestedPercentage.ShouldNotBeNull();

        TermSheetFounder standard = model.Founders[1];
        standard.VestingStartDate.ShouldBeNull();
        standard.VestingMonths.ShouldBeNull();
        standard.VestedPercentage.ShouldBeNull();
    }

    private static string Normalize(string value) => value.Replace("\r\n", "\n");

    private static string ReadFixture(string sampleName)
    {
        Assembly assembly = typeof(TermSheetMarkdownGoldenTests).Assembly;
        string resource = $"{assembly.GetName().Name}.Application.DealDocuments.Golden.term-sheet-{sampleName}.md";
        using Stream stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Golden fixture not found: {resource}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
