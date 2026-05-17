using DevStart.Domain.MediaFiles;
using Shouldly;

namespace DevStart.UnitTests.Domain.MediaFiles;

public sealed class MediaFileTests
{
    [Fact]
    public void Create_ShouldInitializeMediaFile()
    {
        Guid uploaderId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DateTime uploadDate = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        MediaFile file = MediaFile.Create(uploaderId, "avatars/a.png", "media", MediaFileType.Img, 512, uploadDate);

        file.Id.ShouldNotBe(Guid.Empty);
        file.UploaderId.ShouldBe(uploaderId);
        file.ObjectName.ShouldBe("avatars/a.png");
        file.Bucket.ShouldBe("media");
        file.FileType.ShouldBe(MediaFileType.Img);
        file.FileSize.ShouldBe(512);
        file.UploadDate.ShouldBe(uploadDate);
    }
}
