using System.Net;
using System.Net.Http.Json;
using DevStart.Domain.Messages;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Shouldly;

namespace DevStart.IntegrationTests.Messages
{
    /// <summary>
    /// Writing and reading chat as a startup: only a founder or an administrator may do it, and the
    /// human behind a company message stays visible to that company alone.
    /// </summary>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class StartupIdentityTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private sealed record MessageDto(
            Guid Id,
            Guid SenderId,
            int SenderType,
            Guid? SentByProfileId,
            string? TextContent);

        private sealed record ChatIdentityDto(Guid StartupId, string Name, Guid? AvatarId);

        private sealed record ConversationDto(Guid OtherParticipantId, int OtherParticipantType, int UnreadCount);

        [Fact]
        public async Task GetIdentities_ReturnsOnlyStartupsTheCallerMaySpeakFor()
        {
            User founder = await SeedUserAsync();
            User admin = await SeedUserAsync();
            User plainMember = await SeedUserAsync();
            User outsider = await SeedUserAsync();

            Guid startupId = await SeedStartupAsync(
                (founder.Id, StartupRole.Founder),
                (admin.Id, StartupRole.Administration),
                (plainMember.Id, StartupRole.Member));

            (await IdentitiesAsync(founder)).Select(i => i.StartupId).ShouldBe([startupId]);
            (await IdentitiesAsync(admin)).Select(i => i.StartupId).ShouldBe([startupId]);
            (await IdentitiesAsync(plainMember)).ShouldBeEmpty();
            (await IdentitiesAsync(outsider)).ShouldBeEmpty();
        }

        [Fact]
        public async Task SendAsStartup_ArrivesFromTheStartupAndHidesTheAuthorFromTheCounterpart()
        {
            User founder = await SeedUserAsync();
            User counterpart = await SeedUserAsync();
            Guid startupId = await SeedStartupAsync((founder.Id, StartupRole.Founder));

            HttpResponseMessage send = await CreateAuthenticatedClient(founder).PostAsJsonAsync("api/messages", new
            {
                receiverId = counterpart.Id,
                receiverType = (int)ChatParticipantType.User,
                senderStartupId = startupId,
                textContent = "Здравствуйте, пишем от лица компании",
            });
            send.StatusCode.ShouldBe(HttpStatusCode.OK);

            // The counterpart sees the company as the sender, and not who wrote it.
            List<MessageDto>? counterpartThread = await CreateAuthenticatedClient(counterpart)
                .GetFromJsonAsync<List<MessageDto>>(ThreadUrl((int)ChatParticipantType.Startup, startupId));

            counterpartThread.ShouldNotBeNull();
            MessageDto received = counterpartThread.Single();
            received.SenderId.ShouldBe(startupId);
            received.SenderType.ShouldBe((int)ChatParticipantType.Startup);
            received.SentByProfileId.ShouldBeNull();

            // The startup's own side sees the author.
            List<MessageDto>? startupThread = await CreateAuthenticatedClient(founder)
                .GetFromJsonAsync<List<MessageDto>>(
                    ThreadUrl((int)ChatParticipantType.User, counterpart.Id, asStartupId: startupId));

            startupThread.ShouldNotBeNull();
            startupThread.Single().SentByProfileId.ShouldBe(founder.Id);
        }

        [Fact]
        public async Task StartupInboxIsSharedByItsLeadership_AndSeparateFromThePersonalOne()
        {
            User founder = await SeedUserAsync();
            User admin = await SeedUserAsync();
            User counterpart = await SeedUserAsync();
            Guid startupId = await SeedStartupAsync(
                (founder.Id, StartupRole.Founder),
                (admin.Id, StartupRole.Administration));

            HttpResponseMessage inbound = await CreateAuthenticatedClient(counterpart).PostAsJsonAsync("api/messages", new
            {
                receiverId = startupId,
                receiverType = (int)ChatParticipantType.Startup,
                textContent = "Вопрос по инвестициям",
            });
            inbound.StatusCode.ShouldBe(HttpStatusCode.OK);

            // Visible to both leaders under the startup identity...
            foreach (User leader in new[] { founder, admin })
            {
                List<ConversationDto>? conversations = await CreateAuthenticatedClient(leader)
                    .GetFromJsonAsync<List<ConversationDto>>(
                        $"api/messages/conversations?page=1&pageSize=50&asStartupId={startupId}");

                conversations.ShouldNotBeNull();
                conversations.Single().OtherParticipantId.ShouldBe(counterpart.Id);
                conversations.Single().UnreadCount.ShouldBe(1);
            }

            // ...and absent from the founder's personal inbox.
            List<ConversationDto>? personal = await CreateAuthenticatedClient(founder)
                .GetFromJsonAsync<List<ConversationDto>>("api/messages/conversations?page=1&pageSize=50");

            personal.ShouldNotBeNull();
            personal.ShouldBeEmpty();
        }

        [Fact]
        public async Task PlainMember_CanNeitherReadNorWriteAsTheStartup()
        {
            User founder = await SeedUserAsync();
            User plainMember = await SeedUserAsync();
            User counterpart = await SeedUserAsync();
            Guid startupId = await SeedStartupAsync(
                (founder.Id, StartupRole.Founder),
                (plainMember.Id, StartupRole.Member));

            HttpResponseMessage inbound = await CreateAuthenticatedClient(counterpart).PostAsJsonAsync("api/messages", new
            {
                receiverId = startupId,
                receiverType = (int)ChatParticipantType.Startup,
                textContent = "Вопрос по инвестициям",
            });
            inbound.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid messageId = await inbound.Content.ReadFromJsonAsync<Guid>();

            HttpClient memberClient = CreateAuthenticatedClient(plainMember);

            HttpResponseMessage list = await memberClient.GetAsync(
                $"api/messages/conversations?page=1&pageSize=50&asStartupId={startupId}");
            list.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            HttpResponseMessage thread = await memberClient.GetAsync(
                ThreadUrl((int)ChatParticipantType.User, counterpart.Id, asStartupId: startupId));
            thread.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            HttpResponseMessage single = await memberClient.GetAsync($"api/messages/{messageId}");
            single.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

            HttpResponseMessage markRead = await memberClient.PutAsJsonAsync($"api/messages/{messageId}/read", new { });
            markRead.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

            HttpResponseMessage send = await memberClient.PostAsJsonAsync("api/messages", new
            {
                receiverId = counterpart.Id,
                receiverType = (int)ChatParticipantType.User,
                senderStartupId = startupId,
                textContent = "Отвечаю за компанию",
            });
            send.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task LeadershipCanMarkTheStartupsIncomingMessageAsRead()
        {
            User admin = await SeedUserAsync();
            User counterpart = await SeedUserAsync();
            Guid startupId = await SeedStartupAsync((admin.Id, StartupRole.Administration));

            HttpResponseMessage inbound = await CreateAuthenticatedClient(counterpart).PostAsJsonAsync("api/messages", new
            {
                receiverId = startupId,
                receiverType = (int)ChatParticipantType.Startup,
                textContent = "Вопрос",
            });
            Guid messageId = await inbound.Content.ReadFromJsonAsync<Guid>();

            HttpResponseMessage markRead = await CreateAuthenticatedClient(admin)
                .PutAsJsonAsync($"api/messages/{messageId}/read", new { });

            markRead.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            List<ConversationDto>? conversations = await CreateAuthenticatedClient(admin)
                .GetFromJsonAsync<List<ConversationDto>>(
                    $"api/messages/conversations?page=1&pageSize=50&asStartupId={startupId}");
            conversations.ShouldNotBeNull();
            conversations.Single().UnreadCount.ShouldBe(0);
        }

        private async Task<List<ChatIdentityDto>> IdentitiesAsync(User user)
        {
            List<ChatIdentityDto>? identities = await CreateAuthenticatedClient(user)
                .GetFromJsonAsync<List<ChatIdentityDto>>("api/messages/identities");
            identities.ShouldNotBeNull();
            return identities;
        }

        private static string ThreadUrl(int otherType, Guid otherId, Guid? asStartupId = null)
        {
            string url = $"api/messages/conversations/{otherType}/{otherId}?page=1&pageSize=50";
            return asStartupId is null ? url : $"{url}&asStartupId={asStartupId}";
        }

        private async Task<Guid> SeedStartupAsync(params (Guid ProfileId, StartupRole Role)[] members)
        {
            Guid startupId = Guid.NewGuid();
            await ExecuteDbAsync(async db =>
            {
                DateTime now = DateTime.UtcNow;
                db.Startups.Add(new Startup
                {
                    Id = startupId,
                    Name = $"Identity Co {startupId:N}"[..24],
                    PublicEmail = "team@example.com",
                    Stage = StartupStage.Seed,
                    CreatedAt = now,
                    UpdatedAt = now,
                });

                foreach ((Guid profileId, StartupRole role) in members)
                {
                    db.StartupMembers.Add(StartupMember.Create(profileId, startupId, role, isPublic: true, now));
                }

                await db.SaveChangesAsync();
            });
            return startupId;
        }
    }
}
