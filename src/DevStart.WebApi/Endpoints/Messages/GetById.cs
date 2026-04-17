using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Messages.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Messages
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/messages/{messageId:guid}", async (
                Guid messageId,
                IQueryHandler<GetMessageByIdQuery, MessageResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetMessageByIdQuery(messageId);
                Result<MessageResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.MessagesRead)
                .WithTags(Tags.Messages);
        }
    }
}
