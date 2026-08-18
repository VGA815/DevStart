using DevStart.Domain.DealDocuments;
using Shouldly;

namespace DevStart.UnitTests.Domain.DealDocuments;

public sealed class DealDocumentTests
{
    [Fact]
    public void Create_ShouldInitializeDealDocument()
    {
        Guid dealId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DateTime generatedAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        DealDocument document = DealDocument.Create(
            dealId, "term-sheet.md", "term-sheet.pdf", "abc123", "cap-table.csv", generatedAt);

        document.Id.ShouldNotBe(Guid.Empty);
        document.DealId.ShouldBe(dealId);
        document.TermSheetObjectKey.ShouldBe("term-sheet.md");
        document.TermSheetPdfObjectKey.ShouldBe("term-sheet.pdf");
        document.TermSheetPdfSha256.ShouldBe("abc123");
        document.CapTableObjectKey.ShouldBe("cap-table.csv");
        document.GeneratedAt.ShouldBe(generatedAt);
        document.HasPdf.ShouldBeTrue();
    }

    [Fact]
    public void ARowWithoutAPdfKey_ReportsItselfIncomplete()
    {
        var document = new DealDocument { TermSheetPdfObjectKey = string.Empty };

        document.HasPdf.ShouldBeFalse();
    }

    [Fact]
    public void AttachPdf_FillsInTheMissingPdfWithoutTouchingTheRest()
    {
        DateTime generatedAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);
        var document = new DealDocument
        {
            Id = Guid.NewGuid(),
            DealId = Guid.NewGuid(),
            TermSheetObjectKey = "term-sheet.md",
            TermSheetPdfObjectKey = string.Empty,
            TermSheetPdfSha256 = string.Empty,
            CapTableObjectKey = "cap-table.json",
            GeneratedAt = generatedAt,
        };

        DateTime backfilledAt = generatedAt.AddDays(3);
        document.AttachPdf("term-sheet.pdf", "def456", backfilledAt);

        document.HasPdf.ShouldBeTrue();
        document.TermSheetPdfObjectKey.ShouldBe("term-sheet.pdf");
        document.TermSheetPdfSha256.ShouldBe("def456");
        document.TermSheetObjectKey.ShouldBe("term-sheet.md");
        document.CapTableObjectKey.ShouldBe("cap-table.json");
        document.GeneratedAt.ShouldBe(backfilledAt);
    }
}
