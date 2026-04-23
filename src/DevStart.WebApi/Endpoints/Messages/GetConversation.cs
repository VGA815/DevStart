using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Messages.GetById;
using DevStart.Application.Messages.GetConversation;
using DevStart.Domain.Messages;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Messages
{
    internal sealed class GetConversation : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/messages/conversations/{otherType:int}/{otherId:guid}", async (
                int otherType,
                Guid otherId,
                int page,
                int pageSize,
                Guid? asStartupId,
                IQueryHandler<GetConversationQuery, List<MessageResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetConversationQuery(
                    (ChatParticipantType)otherType,
                    otherId,
                    asStartupId,
                    page,
                    pageSize);
                Result<List<MessageResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.MessagesRead)
                .WithTags(Tags.Messages);
        }
    }
}
