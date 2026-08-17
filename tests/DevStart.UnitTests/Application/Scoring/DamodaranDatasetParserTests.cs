using DevStart.Application.Abstractions.Valuation;
using DevStart.Application.Scoring.Benchmarks;
using DevStart.SharedKernel;
using Shouldly;

namespace DevStart.UnitTests.Application.Scoring;

/// <summary>
/// Two behaviours matter here and both are about refusing to be plausible: columns are found by
/// header rather than by position, and an unrecognised layout imports nothing at all.
/// </summary>
public sealed class DamodaranDatasetParserTests
{
    [Fact]
    public void FindsColumnsByHeader_NotByPosition()
    {
        // The value column sits before the industry column, and both carry stray spacing.
        const string csv = """
            Damodaran Online: Industry data
            Updated January 2026

            Number of firms, EV/ Sales ,Industry Name
            120,3.45,Software (System & Application)
            80,1.20,Retail (Online)
            """;

        Result<List<DamodaranBucketObservation>> result = DamodaranDatasetParser.Parse(csv);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].ExternalKey.ShouldBe("Software (System & Application)");
        result.Value[0].EvSales.ShouldBe(3.45m);
        result.Value[1].ExternalKey.ShouldBe("Retail (Online)");
    }

    [Fact]
    public void UnrecognisedLayout_ImportsNothing_AndSaysWhatItExpectedAgainstWhatItFound()
    {
        const string csv = """
            Sector Code,Ratio
            SW,3.45
            """;

        Result<List<DamodaranBucketObservation>> result = DamodaranDatasetParser.Parse(csv);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Damodaran.LayoutUnrecognised");
        result.Error.Description.ShouldContain("ev/sales");
        result.Error.Description.ShouldContain("Sector Code");
        result.Error.Description.ShouldContain("Nothing was imported");
    }

    [Fact]
    public void QuotedFieldsWithCommasSurviveIntact()
    {
        const string csv = """
            Industry Name,EV/Sales
            "Electronics (Consumer, Office)",2.50
            """;

        Result<List<DamodaranBucketObservation>> result = DamodaranDatasetParser.Parse(csv);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Single().ExternalKey.ShouldBe("Electronics (Consumer, Office)");
        result.Value.Single().EvSales.ShouldBe(2.50m);
    }

    [Fact]
    public void UnusableCells_AreSkippedRows_NotAFailedFile()
    {
        const string csv = """
            Industry Name,EV/Sales
            Good Bucket,2.50
            Missing Bucket,NA
            Broken Bucket,#DIV/0!
            Negative Bucket,-1.20
            """;

        Result<List<DamodaranBucketObservation>> result = DamodaranDatasetParser.Parse(csv);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Single().ExternalKey.ShouldBe("Good Bucket");
    }

    [Fact]
    public void ThousandsSeparatorsAreTolerated()
    {
        const string csv = """
            Industry Name,EV/Sales
            Big Bucket,"1,234.50"
            """;

        Result<List<DamodaranBucketObservation>> result = DamodaranDatasetParser.Parse(csv);

        result.Value.Single().EvSales.ShouldBe(1234.50m);
    }

    [Fact]
    public void DuplicateBucketNames_ResolveToTheFirstRow_SoAReparseIsDeterministic()
    {
        const string csv = """
            Industry Name,EV/Sales
            Same Bucket,2.00
            Same Bucket,9.00
            """;

        Result<List<DamodaranBucketObservation>> result = DamodaranDatasetParser.Parse(csv);

        result.Value.Single().EvSales.ShouldBe(2.00m);
    }

    [Fact]
    public void AHeaderWithNoUsableRows_IsAnEmptyDatasetError()
    {
        const string csv = """
            Industry Name,EV/Sales
            Nothing Here,NA
            """;

        Result<List<DamodaranBucketObservation>> result = DamodaranDatasetParser.Parse(csv);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Damodaran.EmptyDataset");
    }

    [Fact]
    public void ALeadingByteOrderMark_DoesNotDefeatHeaderMatching()
    {
        // Spreadsheet exports lead with a BOM. A reader set to detect it strips it, but the parser is
        // public and must not depend on how its caller built the string — otherwise a perfectly good
        // file is rejected as "layout unrecognised" because U+FEFF is glued to the first header cell.
        const string csv = "\uFEFFIndustry Name,EV/Sales\nSoftware (Internet),4.10";

        Result<List<DamodaranBucketObservation>> result = DamodaranDatasetParser.Parse(csv);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Single().ExternalKey.ShouldBe("Software (Internet)");
        result.Value.Single().EvSales.ShouldBe(4.10m);
    }

    [Fact]
    public void ZeroWidthCharactersInsideAHeader_AreIgnored()
    {
        const string csv = "Industry\u200B Name,EV/\u200BSales\nRetail (Online),1.30";

        Result<List<DamodaranBucketObservation>> result = DamodaranDatasetParser.Parse(csv);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Single().ExternalKey.ShouldBe("Retail (Online)");
    }

    [Fact]
    public void AnEmptyFileIsRejectedRatherThanSilentlyImportingNothing()
    {
        Result<List<DamodaranBucketObservation>> result = DamodaranDatasetParser.Parse(string.Empty);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Damodaran.LayoutUnrecognised");
    }
}
