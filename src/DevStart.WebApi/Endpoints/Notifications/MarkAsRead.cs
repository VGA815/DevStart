using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Notifications.MarkAsRead;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Notifications
{
    internal sealed class MarkAsRead : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/notifications/{notificationId:guid}", 
                async (Guid notificationId, ICommandHandler<MarkNotificationAsReadCommand> handler, CancellationToken cancellationToken) =>
                {
                    MarkNotificationAsReadCommand command = new(notificationId);
                    Result result = await handler.Handle(command, cancellationToken);
                    return result.Match(Results.NoContent, CustomResults.Problem);
                })
                    .RequireAuthorization()
                    .WithTags(Tags.Notifications);
        }
    }
}
