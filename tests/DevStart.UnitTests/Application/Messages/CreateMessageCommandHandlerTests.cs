using DevStart.Application.Messages.Create;
using DevStart.Domain.ChatFiles;
using DevStart.Domain.Messages;
using DevStart.Domain.Profiles;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.Messages;

public sealed class CreateMessageCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldPersistAttachedStartupDocument()
    {
        await using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (User sender, User receiver, Startup startup) = await SeedAsync(context);
        StartupDocumentFile document = SeedDocument(context, startup.Id, sender.Id);
        await context.SaveChangesAsync();

        Result<Guid> result = await CreateHandler(context, sender.Id).Handle(
            new CreateMessageCommand
            {
                ReceiverId = receiver.Id,
                ReceiverType = ChatParticipantType.User,
                TextContent = "Вот наш питч",
                DocumentIds = [document.Id],
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        Message message = context.Messages.Single();
        message.DocumentIds.ShouldBe([document.Id]);
    }

    [Fact]
    public async Task Handle_ShouldRejectDocumentOfAStartupTheSenderIsNotAMemberOf()
    {
        await using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (User sender, User receiver, _) = await SeedAsync(context);

        Startup foreignStartup = CreateStartup("Foreign");
        context.Startups.Add(foreignStartup);
        StartupDocumentFile foreignDocument = SeedDocument(context, foreignStartup.Id, receiver.Id);
        await context.SaveChangesAsync();

        Result<Guid> result = await CreateHandler(context, sender.Id).Handle(
            new CreateMessageCommand
            {
                ReceiverId = receiver.Id,
                ReceiverType = ChatParticipantType.User,
                DocumentIds = [foreignDocument.Id],
            },
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Messages.AttachmentNotAllowed");
        context.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldBindAttachedChatFileToTheMessage()
    {
        await using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (User sender, User receiver, _) = await SeedAsync(context);

        ChatFile chatFile = ChatFile.Create(
            Guid.NewGuid(), sender.Id, "chat/x/y.pdf", ChatFileRules.Bucket, "y.pdf", "application/pdf", 10, UtcNow);
        context.ChatFiles.Add(chatFile);
        await context.SaveChangesAsync();

        Result<Guid> result = await CreateHandler(context, sender.Id).Handle(
            new CreateMessageCommand
            {
                ReceiverId = receiver.Id,
                ReceiverType = ChatParticipantType.User,
                FileIds = [chatFile.Id],
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        context.ChatFiles.Single().MessageId.ShouldBe(result.Value);
        context.Messages.Single().FileIds.ShouldBe([chatFile.Id]);
    }

    [Fact]
    public async Task Handle_ShouldRejectChatFileUploadedBySomeoneElse()
    {
        await using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (User sender, User receiver, _) = await SeedAsync(context);

        ChatFile foreignFile = ChatFile.Create(
            Guid.NewGuid(), receiver.Id, "chat/x/y.pdf", ChatFileRules.Bucket, "y.pdf", "application/pdf", 10, UtcNow);
        context.ChatFiles.Add(foreignFile);
        await context.SaveChangesAsync();

        Result<Guid> result = await CreateHandler(context, sender.Id).Handle(
            new CreateMessageCommand
            {
                ReceiverId = receiver.Id,
                ReceiverType = ChatParticipantType.User,
                FileIds = [foreignFile.Id],
            },
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Messages.AttachmentNotAllowed");
    }

    [Fact]
    public async Task Handle_ShouldRejectWhitespaceOnlyMessageWithoutAttachments()
    {
        await using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (User sender, User receiver, _) = await SeedAsync(context);

        Result<Guid> result = await CreateHandler(context, sender.Id).Handle(
            new CreateMessageCommand
            {
                ReceiverId = receiver.Id,
                ReceiverType = ChatParticipantType.User,
                TextContent = "   \n  ",
            },
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(MessageErrors.IsEmpty);
    }

    private static CreateMessageCommandHandler CreateHandler(ApplicationDbContext context, Guid userId) =>
        new(context, new TestUserContext(userId), new FixedDateTimeProvider { UtcNow = UtcNow });

    private static async Task<(User Sender, User Receiver, Startup Startup)> SeedAsync(ApplicationDbContext context)
    {
        User sender = CreateUser("sender");
        User receiver = CreateUser("receiver");
        Startup startup = CreateStartup("DevStart");

        context.Users.AddRange(sender, receiver);
        context.Profiles.AddRange(
            Profile.Create(sender.Id, "Sender", null, null, false, true, null),
            Profile.Create(receiver.Id, "Receiver", null, null, false, true, null));
        context.Startups.Add(startup);
        context.StartupMembers.Add(StartupMember.Create(sender.Id, startup.Id, StartupRole.Founder, true, UtcNow));

        await context.SaveChangesAsync();

        return (sender, receiver, startup);
    }

    private static StartupDocumentFile SeedDocument(ApplicationDbContext context, Guid startupId, Guid uploaderId)
    {
        StartupDocumentFile document = StartupDocumentFile.Create(
            Guid.NewGuid(),
            startupId,
            uploaderId,
            $"startups/{startupId}/doc",
            "startup-documents",
            StartupDocumentType.Pitch,
            1024,
            "Pitch deck",
            UtcNow);

        context.StartupDocumentFiles.Add(document);
        return document;
    }

    private static User CreateUser(string name)
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        return User.Create($"{name}_{suffix}", $"{name}_{suffix}@devstart.test", "hash", UtcNow);
    }

    private static Startup CreateStartup(string name) => Startup.Create(
        name,
        "public@example.com",
        "Description",
        "https://example.com",
        StartupStage.Mvp,
        StartupLocation.Russia,
        "billing@example.com",
        avatarId: null,
        createdAt: UtcNow,
        socialMediaLinks: [],
        shortDescription: "Short");
}
