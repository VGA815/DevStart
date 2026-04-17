using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Messages.GetConversation;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Messages
{
    internal sealed class GetConversation : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/messages/conversations/{otherUserId:guid}", async (
                Guid otherUserId,
                int page,
                int pageSize,
                IQueryHandler<GetConversationQuery, List<MessageResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetConversationQuery(otherUserId, page, pageSize);
                Result<List<MessageResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.MessagesRead)
                .WithTags(Tags.Messages);
        }
    }
}
