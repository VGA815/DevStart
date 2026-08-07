using DevStart.Application.Abstractions.Data;
using DevStart.Application.MediaFiles.Upload;
using DevStart.Domain.MediaFiles;
using DevStart.Domain.Users;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.MediaFiles;

public sealed class UploadMediaFileCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUploadAvatarWithObjectKeyWithoutLeadingSlash()
    {
        Guid userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DateTime utcNow = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

        await using var context = InMemoryDbContextFactory.Create();
        User user = User.Create("user", "user@example.com", "hash", utcNow);
        user.Id = userId;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var fileStorage = new CapturingFileStorage();
        var dateTimeProvider = new FixedDateTimeProvider { UtcNow = utcNow };
        var handler = new UploadMediaFileCommandHandler(
            context,
            new TestUserContext(userId),
            fileStorage,
            dateTimeProvider);

        var command = new UploadMediaFileCommand(
            userId,
            new MemoryStream([1, 2, 3]),
            "image/webp",
            3,
            "avatars");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        CapturingFileStorage.UploadCall upload = fileStorage.Uploads.Single();
        upload.Bucket.ShouldBe("avatars");
        upload.ObjectKey.ShouldStartWith($"users/{userId}/");
        upload.ObjectKey.ShouldEndWith(".webp");
        upload.ObjectKey.StartsWith('/').ShouldBeFalse();

        MediaFile mediaFile = context.MediaFiles.Single();
        mediaFile.Id.ShouldBe(result.Value);
        mediaFile.ObjectName.ShouldBe(upload.ObjectKey);
        mediaFile.Bucket.ShouldBe("avatars");
        mediaFile.UploadDate.ShouldBe(utcNow);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("application/x-msdownload")]
    [InlineData("text/plain")]
    public async Task Handle_ShouldRejectNonImageUploads(string contentType)
    {
        Guid userId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        DateTime utcNow = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

        await using var context = InMemoryDbContextFactory.Create();
        User user = User.Create("user", "user@example.com", "hash", utcNow);
        user.Id = userId;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var fileStorage = new CapturingFileStorage();
        var handler = new UploadMediaFileCommandHandler(
            context,
            new TestUserContext(userId),
            fileStorage,
            new FixedDateTimeProvider { UtcNow = utcNow });

        var result = await handler.Handle(
            new UploadMediaFileCommand(userId, new MemoryStream([1, 2, 3]), contentType, 3, "avatars"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(MediaFileErrors.ContentTypeNotAllowed);
        fileStorage.Uploads.ShouldBeEmpty();
        context.MediaFiles.Any().ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldRejectImageOverTheSizeLimit()
    {
        Guid userId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        DateTime utcNow = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

        await using var context = InMemoryDbContextFactory.Create();
        User user = User.Create("user", "user@example.com", "hash", utcNow);
        user.Id = userId;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UploadMediaFileCommandHandler(
            context,
            new TestUserContext(userId),
            new CapturingFileStorage(),
            new FixedDateTimeProvider { UtcNow = utcNow });

        var result = await handler.Handle(
            new UploadMediaFileCommand(
                userId, new MemoryStream([1]), "image/png", MediaFileRules.MaxFileSizeBytes + 1, "avatars"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(MediaFileErrors.TooLarge);
    }

    [Fact]
    public async Task Handle_ShouldNameTheObjectAfterTheActualContentType()
    {
        Guid userId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        DateTime utcNow = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

        await using var context = InMemoryDbContextFactory.Create();
        User user = User.Create("user", "user@example.com", "hash", utcNow);
        user.Id = userId;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var fileStorage = new CapturingFileStorage();
        var handler = new UploadMediaFileCommandHandler(
            context,
            new TestUserContext(userId),
            fileStorage,
            new FixedDateTimeProvider { UtcNow = utcNow });

        var result = await handler.Handle(
            new UploadMediaFileCommand(userId, new MemoryStream([1, 2, 3]), "image/gif", 3, "avatars"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        fileStorage.Uploads.Single().ObjectKey.ShouldEndWith(".gif");
        context.MediaFiles.Single().FileType.ShouldBe(MediaFileType.Gif);
    }

    [Fact]
    public async Task Handle_StorageUnavailable_ReturnsStorageUnavailableAndPersistsNothing()
    {
        Guid userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        DateTime utcNow = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

        await using var context = InMemoryDbContextFactory.Create();
        User user = User.Create("user", "user@example.com", "hash", utcNow);
        user.Id = userId;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var fileStorage = new CapturingFileStorage
        {
            UploadException = new FileStorageException("storage down", notFound: false)
        };
        var handler = new UploadMediaFileCommandHandler(
            context,
            new TestUserContext(userId),
            fileStorage,
            new FixedDateTimeProvider { UtcNow = utcNow });

        var command = new UploadMediaFileCommand(
            userId, new MemoryStream([1, 2, 3]), "image/webp", 3, "avatars");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(MediaFileErrors.StorageUnavailable);
        // The media-file row must not be persisted when the object never landed in storage.
        context.MediaFiles.Any().ShouldBeFalse();
    }
}
