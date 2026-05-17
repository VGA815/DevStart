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

        DealDocument document = DealDocument.Create(dealId, "term-sheet.md", "cap-table.csv", generatedAt);

        document.Id.ShouldNotBe(Guid.Empty);
        document.DealId.ShouldBe(dealId);
        document.TermSheetObjectKey.ShouldBe("term-sheet.md");
        document.CapTableObjectKey.ShouldBe("cap-table.csv");
        document.GeneratedAt.ShouldBe(generatedAt);
    }
}
