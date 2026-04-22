using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Notifications.MarkAllAsRead;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Notifications
{
    internal sealed class MarkAllAsRead : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/notifications/read-all", async (
                ICommandHandler<MarkAllNotificationsAsReadCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(new MarkAllNotificationsAsReadCommand(), cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.NotificationsUpdate)
                .WithTags(Tags.Notifications);
        }
    }
}
