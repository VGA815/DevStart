using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Notifications.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Notifications
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/notifications/{notificationId:guid}", async (
                Guid notificationId, 
                IQueryHandler<GetNotificationByIdQuery, NotificationResponse> handler,
                CancellationToken cancellationToken) => 
            { 
                var query = new GetNotificationByIdQuery(notificationId);
                Result<NotificationResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.NotificationsRead)
                .WithTags(Tags.Notifications);
        }
    }
}
