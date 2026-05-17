using DevStart.Domain.ConsentDocuments;
using DevStart.Domain.UserConsents;
using Shouldly;

namespace DevStart.UnitTests.Domain.ConsentDocuments;

public sealed class ConsentDocumentTests
{
    [Fact]
    public void Create_ShouldInitializeInactiveDocument()
    {
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        ConsentDocument document = ConsentDocument.Create(
            ConsentType.Cookies,
            "v1",
            "Cookies",
            "Content",
            createdAt);

        document.Id.ShouldNotBe(Guid.Empty);
        document.Type.ShouldBe(ConsentType.Cookies);
        document.Version.ShouldBe("v1");
        document.Title.ShouldBe("Cookies");
        document.Content.ShouldBe("Content");
        document.CreatedAt.ShouldBe(createdAt);
        document.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void ActivateAndDeactivate_ShouldToggleActiveState()
    {
        ConsentDocument document = ConsentDocument.Create(
            ConsentType.Cookies,
            "v1",
            "Cookies",
            "Content",
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc));

        document.Activate();
        document.IsActive.ShouldBeTrue();

        document.Deactivate();
        document.IsActive.ShouldBeFalse();
    }
}
