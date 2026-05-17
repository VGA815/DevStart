using DevStart.Domain.StartupDocumentFiles;
using Shouldly;

namespace DevStart.UnitTests.Domain.StartupDocumentFiles;

public sealed class StartupDocumentFileTests
{
    [Fact]
    public void Create_ShouldInitializeStartupDocumentFile()
    {
        Guid id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid startupId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid uploaderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        DateTime uploadDate = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        StartupDocumentFile file = StartupDocumentFile.Create(
            id,
            startupId,
            uploaderId,
            "docs/pitch.pdf",
            "startup-documents",
            StartupDocumentType.Pitch,
            1024,
            "Pitch deck",
            uploadDate);

        file.Id.ShouldBe(id);
        file.StartupId.ShouldBe(startupId);
        file.UploaderId.ShouldBe(uploaderId);
        file.ObjectName.ShouldBe("docs/pitch.pdf");
        file.Bucket.ShouldBe("startup-documents");
        file.DocumentType.ShouldBe(StartupDocumentType.Pitch);
        file.FileSize.ShouldBe(1024);
        file.DocumentName.ShouldBe("Pitch deck");
        file.UploadDate.ShouldBe(uploadDate);
    }
}
