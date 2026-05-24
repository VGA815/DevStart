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
}
