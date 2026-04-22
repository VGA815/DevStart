using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Notifications.GetUnreadCount;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Notifications
{
    internal sealed class GetUnreadCount : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/notifications/unread-count", async (
                IQueryHandler<GetUnreadCountQuery, int> handler,
                CancellationToken cancellationToken) =>
            {
                Result<int> result = await handler.Handle(new GetUnreadCountQuery(), cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.NotificationsRead)
                .WithTags(Tags.Notifications);
        }
    }
}
