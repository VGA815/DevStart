using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Messages.MarkAsRead;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Messages
{
    internal sealed class MarkAsRead : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/messages/{messageId:guid}/read", async (
                Guid messageId,
                ICommandHandler<MarkMessageAsReadCommand> handler,
                CancellationToken cancellationToken) =>
            {
                MarkMessageAsReadCommand command = new(messageId);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.MessagesUpdate)
                .WithTags(Tags.Messages);
        }
    }
}
