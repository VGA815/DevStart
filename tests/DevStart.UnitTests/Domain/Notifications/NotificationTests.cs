using DevStart.Domain.Notifications;
using Shouldly;

namespace DevStart.UnitTests.Domain.Notifications;

public sealed class NotificationTests
{
    [Fact]
    public void Create_ShouldInitializeUnreadNotification()
    {
        Guid userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid referenceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        Notification notification = Notification.Create(
            userId,
            NotificationType.MessageReceived,
            "Title",
            "Body",
            createdAt,
            referenceId);

        notification.Id.ShouldNotBe(Guid.Empty);
        notification.UserId.ShouldBe(userId);
        notification.Type.ShouldBe(NotificationType.MessageReceived);
        notification.Title.ShouldBe("Title");
        notification.Body.ShouldBe("Body");
        notification.ReferenceId.ShouldBe(referenceId);
        notification.CreatedAt.ShouldBe(createdAt);
        notification.IsRead.ShouldBeFalse();
    }

    [Fact]
    public void MarkAsRead_ShouldMarkNotificationAsRead()
    {
        Notification notification = Notification.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            NotificationType.Welcome,
            "Title",
            "Body",
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc));

        notification.MarkAsRead();

        notification.IsRead.ShouldBeTrue();
    }
}
