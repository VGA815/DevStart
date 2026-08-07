using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DevStart.Domain.ChatFiles;
using DevStart.Domain.Messages;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Messages
{
    /// <summary>
    /// Covers the two attachment paths the chat composer offers: a document already uploaded to a
    /// startup, and a file picked from the sender's machine.
    /// </summary>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class MessageAttachmentTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private sealed record MessageDto(
            Guid Id,
            Guid SenderId,
            int SenderType,
            Guid ReceiverId,
            int ReceiverType,
            string? TextContent,
            List<Guid> MediaIds,
            List<Guid> MetricIds,
            List<Guid> DocumentIds,
            List<Guid> FileIds,
            bool IsRead);

        private sealed record ChatFileDto(
            Guid Id,
            Guid UploaderId,
            string FileName,
            string ContentType,
            long FileSize,
            string PresignedUrl);

        [Fact]
        public async Task SendMessage_WithStartupDocument_PersistsAndReturnsTheAttachment()
        {
            User sender = await SeedUserAsync();
            User receiver = await SeedUserAsync();
            Guid startupId = await SeedStartupWithMemberAsync(sender.Id);
            Guid documentId = await SeedDocumentAsync(startupId, sender.Id);

            HttpClient client = CreateAuthenticatedClient(sender);

            HttpResponseMessage send = await client.PostAsJsonAsync("api/messages", new
            {
                receiverId = receiver.Id,
                receiverType = (int)ChatParticipantType.User,
                textContent = "Our pitch deck",
                documentIds = new[] { documentId },
            });

            send.StatusCode.ShouldBe(HttpStatusCode.OK);

            await ExecuteDbAsync(async db =>
            {
                Message message = await db.Messages.SingleAsync();
                message.DocumentIds.ShouldBe([documentId]);
            });

            List<MessageDto>? thread = await client.GetFromJsonAsync<List<MessageDto>>(
                $"api/messages/conversations/{(int)ChatParticipantType.User}/{receiver.Id}?page=1&pageSize=50");

            thread.ShouldNotBeNull();
            thread.Single().DocumentIds.ShouldBe([documentId]);
        }

        [Fact]
        public async Task SendMessage_WithDocumentOfAForeignStartup_IsRejected()
        {
            User sender = await SeedUserAsync();
            User outsider = await SeedUserAsync();
            Guid foreignStartupId = await SeedStartupWithMemberAsync(outsider.Id);
            Guid foreignDocumentId = await SeedDocumentAsync(foreignStartupId, outsider.Id);

            HttpResponseMessage send = await CreateAuthenticatedClient(sender).PostAsJsonAsync("api/messages", new
            {
                receiverId = outsider.Id,
                receiverType = (int)ChatParticipantType.User,
                documentIds = new[] { foreignDocumentId },
            });

            send.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            await ExecuteDbAsync(async db => (await db.Messages.AnyAsync()).ShouldBeFalse());
        }

        [Fact]
        public async Task UploadChatFile_ThenSend_LetsTheRecipientReadItButNotAThirdParty()
        {
            User sender = await SeedUserAsync();
            User receiver = await SeedUserAsync();
            User stranger = await SeedUserAsync();

            HttpClient senderClient = CreateAuthenticatedClient(sender);

            ChatFileDto? uploaded = await UploadAsync(senderClient, "report.pdf", "application/pdf");
            uploaded.ShouldNotBeNull();
            uploaded.FileName.ShouldBe("report.pdf");

            // Before it is sent the file belongs to its uploader alone.
            HttpResponseMessage strangerBeforeSend = await CreateAuthenticatedClient(stranger)
                .GetAsync($"api/chat/files/{uploaded.Id}");
            strangerBeforeSend.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            HttpResponseMessage send = await senderClient.PostAsJsonAsync("api/messages", new
            {
                receiverId = receiver.Id,
                receiverType = (int)ChatParticipantType.User,
                fileIds = new[] { uploaded.Id },
            });
            send.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid messageId = await send.Content.ReadFromJsonAsync<Guid>();

            await ExecuteDbAsync(async db =>
            {
                ChatFile file = await db.ChatFiles.SingleAsync();
                file.MessageId.ShouldBe(messageId);
                file.Bucket.ShouldBe(ChatFileRules.Bucket);
            });

            HttpResponseMessage recipientRead = await CreateAuthenticatedClient(receiver)
                .GetAsync($"api/chat/files/{uploaded.Id}");
            recipientRead.StatusCode.ShouldBe(HttpStatusCode.OK);

            HttpResponseMessage strangerRead = await CreateAuthenticatedClient(stranger)
                .GetAsync($"api/chat/files/{uploaded.Id}");
            strangerRead.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UploadChatFile_WithDisallowedContentType_IsRejected()
        {
            User sender = await SeedUserAsync();

            HttpResponseMessage response = await UploadRawAsync(
                CreateAuthenticatedClient(sender), "payload.exe", "application/x-msdownload");

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            await ExecuteDbAsync(async db => (await db.ChatFiles.AnyAsync()).ShouldBeFalse());
        }

        [Fact]
        public async Task UploadChatFile_WithoutToken_IsRejected()
        {
            HttpResponseMessage response = await UploadRawAsync(CreateClient(), "report.pdf", "application/pdf");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetDocumentsByUploader_ForAnotherUser_IsRejected()
        {
            User owner = await SeedUserAsync();
            User other = await SeedUserAsync();
            Guid startupId = await SeedStartupWithMemberAsync(owner.Id);
            await SeedDocumentAsync(startupId, owner.Id);

            HttpResponseMessage foreignRead = await CreateAuthenticatedClient(other)
                .GetAsync($"api/users/{owner.Id}/documents");
            foreignRead.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            HttpResponseMessage ownRead = await CreateAuthenticatedClient(owner)
                .GetAsync($"api/users/{owner.Id}/documents");
            ownRead.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        private static async Task<ChatFileDto?> UploadAsync(HttpClient client, string fileName, string contentType)
        {
            HttpResponseMessage response = await UploadRawAsync(client, fileName, contentType);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            return await response.Content.ReadFromJsonAsync<ChatFileDto>();
        }

        private static Task<HttpResponseMessage> UploadRawAsync(HttpClient client, string fileName, string contentType)
        {
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent([1, 2, 3, 4]);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            return client.PostAsync("api/chat/files", content);
        }

        private async Task<Guid> SeedStartupWithMemberAsync(Guid profileId)
        {
            Guid startupId = Guid.NewGuid();
            await ExecuteDbAsync(async db =>
            {
                DateTime now = DateTime.UtcNow;
                db.Startups.Add(new Startup
                {
                    Id = startupId,
                    Name = $"Chat Co {startupId:N}"[..20],
                    PublicEmail = "chat@example.com",
                    Stage = StartupStage.Seed,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                db.StartupMembers.Add(StartupMember.Create(profileId, startupId, StartupRole.Founder, isPublic: true, now));
                await db.SaveChangesAsync();
            });
            return startupId;
        }

        private async Task<Guid> SeedDocumentAsync(Guid startupId, Guid uploaderId)
        {
            Guid documentId = Guid.NewGuid();
            await ExecuteDbAsync(async db =>
            {
                db.StartupDocumentFiles.Add(StartupDocumentFile.Create(
                    documentId,
                    startupId,
                    uploaderId,
                    $"startups/{startupId}/{documentId}",
                    "startup-documents",
                    StartupDocumentType.Pitch,
                    2048,
                    "Pitch deck",
                    DateTime.UtcNow));
                await db.SaveChangesAsync();
            });
            return documentId;
        }
    }
}
