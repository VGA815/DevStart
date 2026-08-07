using System.Net;
using System.Net.Http.Json;
using DevStart.Domain.Messages;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Shouldly;

namespace DevStart.IntegrationTests.Messages
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class MessagePagingTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private sealed record MessageDto(Guid Id, string? TextContent);

        [Fact]
        public async Task GetConversation_WithPageBelowOne_FallsBackToTheFirstPage()
        {
            (HttpClient client, Guid otherId) = await SeedConversationAsync(messageCount: 3);

            HttpResponseMessage response = await client.GetAsync(ConversationUrl(otherId, page: 0, pageSize: 50));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            List<MessageDto>? messages = await response.Content.ReadFromJsonAsync<List<MessageDto>>();
            messages.ShouldNotBeNull();
            messages.Count.ShouldBe(3);
        }

        [Fact]
        public async Task GetConversations_WithNegativePaging_FallsBackToTheFirstPage()
        {
            (HttpClient client, _) = await SeedConversationAsync(messageCount: 1);

            HttpResponseMessage response = await client.GetAsync("api/messages/conversations?page=-1&pageSize=0");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetConversation_PagesBackwardsThroughHistory()
        {
            (HttpClient client, Guid otherId) = await SeedConversationAsync(messageCount: 5);

            List<MessageDto>? firstPage = await client.GetFromJsonAsync<List<MessageDto>>(
                ConversationUrl(otherId, page: 1, pageSize: 2));
            List<MessageDto>? secondPage = await client.GetFromJsonAsync<List<MessageDto>>(
                ConversationUrl(otherId, page: 2, pageSize: 2));

            firstPage.ShouldNotBeNull();
            secondPage.ShouldNotBeNull();
            firstPage.Count.ShouldBe(2);
            secondPage.Count.ShouldBe(2);

            // Newest first, and the pages must not overlap.
            firstPage.Select(m => m.TextContent).ShouldBe(["msg-4", "msg-3"]);
            secondPage.Select(m => m.TextContent).ShouldBe(["msg-2", "msg-1"]);
        }

        private static string ConversationUrl(Guid otherId, int page, int pageSize) =>
            $"api/messages/conversations/{(int)ChatParticipantType.User}/{otherId}?page={page}&pageSize={pageSize}";

        private async Task<(HttpClient Client, Guid OtherId)> SeedConversationAsync(int messageCount)
        {
            User sender = await SeedUserAsync();
            User receiver = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(sender);

            for (int i = 0; i < messageCount; i++)
            {
                HttpResponseMessage sent = await client.PostAsJsonAsync("api/messages", new
                {
                    receiverId = receiver.Id,
                    receiverType = (int)ChatParticipantType.User,
                    textContent = $"msg-{i}",
                });
                sent.StatusCode.ShouldBe(HttpStatusCode.OK);
            }

            return (client, receiver.Id);
        }
    }
}
