using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Notifications;
using DevStart.Application.Notifications.GetByUserId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Notifications
{
    internal sealed class GetByUserId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/notifications", async (
                bool? isRead,
                int page,
                int pageSize,
                IQueryHandler<GetNotificationsByUserIdQuery, List<NotificationResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetNotificationsByUserIdQuery(isRead, page, pageSize);
                Result<List<NotificationResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.NotificationsRead)
                .WithTags(Tags.Notifications);
        }
    }
}
