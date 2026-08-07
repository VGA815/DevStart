using DevStart.Application.ChatFiles;
using DevStart.Application.ChatFiles.Upload;
using DevStart.Domain.ChatFiles;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.ChatFiles;

public sealed class UploadChatFileCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTime UtcNow = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldStoreFileInChatBucketWithOriginalNameAndContentType()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var fileStorage = new CapturingFileStorage();
        UploadChatFileCommandHandler handler = CreateHandler(context, fileStorage);

        var command = new UploadChatFileCommand(
            new MemoryStream([1, 2, 3, 4]),
            "C:\\Users\\vga\\Desktop\\Питч дек.pdf",
            "application/pdf",
            4);

        Result<ChatFileResponse> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.FileName.ShouldBe("Питч дек.pdf");
        result.Value.ContentType.ShouldBe("application/pdf");
        result.Value.PresignedUrl.ShouldNotBeNullOrWhiteSpace();

        CapturingFileStorage.UploadCall upload = fileStorage.Uploads.Single();
        upload.Bucket.ShouldBe(ChatFileRules.Bucket);
        upload.ObjectKey.ShouldBe($"chat/{UserId}/{result.Value.Id}.pdf");
        upload.ContentType.ShouldBe("application/pdf");

        ChatFile stored = context.ChatFiles.Single();
        stored.UploaderId.ShouldBe(UserId);
        stored.MessageId.ShouldBeNull();
        stored.UploadDate.ShouldBe(UtcNow);
    }

    [Fact]
    public async Task Handle_ShouldRejectDisallowedContentType()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var fileStorage = new CapturingFileStorage();
        UploadChatFileCommandHandler handler = CreateHandler(context, fileStorage);

        var command = new UploadChatFileCommand(
            new MemoryStream([1]),
            "payload.exe",
            "application/x-msdownload",
            1);

        Result<ChatFileResponse> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ChatFileErrors.ContentTypeNotAllowed);
        fileStorage.Uploads.ShouldBeEmpty();
        context.ChatFiles.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldRejectFileOverSizeLimit()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var fileStorage = new CapturingFileStorage();
        UploadChatFileCommandHandler handler = CreateHandler(context, fileStorage);

        var command = new UploadChatFileCommand(
            new MemoryStream([1]),
            "huge.pdf",
            "application/pdf",
            ChatFileRules.MaxFileSizeBytes + 1);

        Result<ChatFileResponse> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ChatFileErrors.TooLarge);
        fileStorage.Uploads.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldRejectEmptyFile()
    {
        await using var context = InMemoryDbContextFactory.Create();
        UploadChatFileCommandHandler handler = CreateHandler(context, new CapturingFileStorage());

        Result<ChatFileResponse> result = await handler.Handle(
            new UploadChatFileCommand(new MemoryStream(), "empty.pdf", "application/pdf", 0),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ChatFileErrors.Empty);
    }

    private static UploadChatFileCommandHandler CreateHandler(
        ApplicationDbContext context,
        CapturingFileStorage fileStorage) =>
        new(context, new TestUserContext(UserId), fileStorage, new FixedDateTimeProvider { UtcNow = UtcNow });
}
