using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Notifications;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Notifications
{
    internal sealed class GetCentrifugoToken : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/notifications/centrifugo-token", (
                IUserContext userContext,
                ICentrifugoTokenProvider tokenProvider) =>
            {
                string token = tokenProvider.CreateConnectionToken(userContext.UserId);
                return Results.Ok(new { token });
            })
                .HasPermission(Permissions.NotificationsRead)
                .WithTags(Tags.Notifications);
        }
    }
}
