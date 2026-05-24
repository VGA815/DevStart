using DevStart.Application.StartupDocumentFiles.Upload;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.StartupDocumentFiles;

public sealed class UploadStartupDocumentFileCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUploadDocumentWithObjectKeyWithoutLeadingSlash()
    {
        Guid userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid startupId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        DateTime utcNow = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

        await using var context = InMemoryDbContextFactory.Create();
        Startup startup = Startup.Create(
            "DevStart",
            "public@example.com",
            "Description",
            "https://example.com",
            StartupStage.Mvp,
            StartupLocation.Russia,
            "billing@example.com",
            avatarId: null,
            createdAt: utcNow,
            socialMediaLinks: [],
            shortDescription: "Short");
        startup.Id = startupId;

        context.Startups.Add(startup);
        context.StartupMembers.Add(StartupMember.Create(
            userId,
            startupId,
            StartupRole.Founder,
            isPublic: true,
            utcNow));
        await context.SaveChangesAsync();

        var fileStorage = new CapturingFileStorage();
        var dateTimeProvider = new FixedDateTimeProvider { UtcNow = utcNow };
        var handler = new UploadStartupDocumentFileCommandHandler(
            context,
            new TestUserContext(userId),
            dateTimeProvider,
            fileStorage);

        var command = new UploadStartupDocumentFileCommand(
            startupId,
            StartupDocumentType.Pitch,
            3,
            new MemoryStream([1, 2, 3]),
            "application/pdf",
            "startup-documents",
            "Pitch deck");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        CapturingFileStorage.UploadCall upload = fileStorage.Uploads.Single();
        upload.Bucket.ShouldBe("startup-documents");
        upload.ObjectKey.ShouldStartWith($"startups/{startupId}/");
        upload.ObjectKey.StartsWith('/').ShouldBeFalse();

        StartupDocumentFile document = context.StartupDocumentFiles.Single();
        document.Id.ShouldBe(result.Value);
        document.ObjectName.ShouldBe(upload.ObjectKey);
        document.Bucket.ShouldBe("startup-documents");
        document.UploadDate.ShouldBe(utcNow);
    }
}
