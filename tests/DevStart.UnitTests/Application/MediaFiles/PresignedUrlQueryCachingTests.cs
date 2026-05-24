using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.MediaFiles.GetById;
using DevStart.Application.StartupDocumentFiles.GetById;
using Shouldly;

namespace DevStart.UnitTests.Application.MediaFiles;

public sealed class PresignedUrlQueryCachingTests
{
    [Fact]
    public void MediaFileQuery_ShouldNotBeCacheable()
    {
        object query = new GetMediaFileByIdQuery(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Expires: 600);

        (query is ICacheableQuery).ShouldBeFalse();
    }

    [Fact]
    public void StartupDocumentFileQuery_ShouldNotBeCacheable()
    {
        object query = new GetStartupDocumentFileByIdQuery(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Expires: 600);

        (query is ICacheableQuery).ShouldBeFalse();
    }
}
