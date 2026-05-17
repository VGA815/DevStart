using DevStart.Domain.Messages;
using Shouldly;

namespace DevStart.UnitTests.Domain.Messages;

public sealed class MessageTests
{
    [Fact]
    public void Create_ShouldInitializeUnreadMessageWithProvidedAttachments()
    {
        Guid senderId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid receiverId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid mediaId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        Guid metricId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        Message message = Message.Create(
            senderId,
            ChatParticipantType.User,
            receiverId,
            ChatParticipantType.Startup,
            "Hello",
            [mediaId],
            [metricId],
            createdAt);

        message.Id.ShouldNotBe(Guid.Empty);
        message.SenderId.ShouldBe(senderId);
        message.SenderType.ShouldBe(ChatParticipantType.User);
        message.ReceiverId.ShouldBe(receiverId);
        message.ReceiverType.ShouldBe(ChatParticipantType.Startup);
        message.TextContent.ShouldBe("Hello");
        message.MediaIds.ShouldBe([mediaId]);
        message.MetricIds.ShouldBe([metricId]);
        message.IsRead.ShouldBeFalse();
        message.CreatedAt.ShouldBe(createdAt);
        message.UpdatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void Create_ShouldUseEmptyAttachmentLists_WhenNullListsAreProvided()
    {
        Message message = Message.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ChatParticipantType.User,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ChatParticipantType.Startup,
            textContent: null,
            mediaIds: null,
            metricIds: null,
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc));

        message.MediaIds.ShouldBeEmpty();
        message.MetricIds.ShouldBeEmpty();
    }

    [Fact]
    public void MarkAsRead_ShouldMarkMessageAsRead()
    {
        Message message = Message.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ChatParticipantType.User,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ChatParticipantType.Startup,
            "Hello",
            mediaIds: null,
            metricIds: null,
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc));

        message.MarkAsRead();

        message.IsRead.ShouldBeTrue();
    }
}
