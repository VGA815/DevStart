using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Messages.GetConversations;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Messages
{
    internal sealed class GetConversations : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/messages/conversations", async (
                int page,
                int pageSize,
                Guid? asStartupId,
                IQueryHandler<GetConversationsQuery, List<ConversationSummaryResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetConversationsQuery(page, pageSize, asStartupId);
                Result<List<ConversationSummaryResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.MessagesRead)
                .WithTags(Tags.Messages);
        }
    }
}
