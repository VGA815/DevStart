
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.UserPreferences.Update;
using DevStart.Domain.UserPreferences;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.UserPreferences
{
    internal sealed class Update : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("user_id")] Guid UserId,
            [property: JsonPropertyName("theme")] UserPreferenceTheme Theme,
            [property: JsonPropertyName("receive_notifications")] bool ReceiveNotifications);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("users/preferences", async (
                [FromBody] Request request, 
                ICommandHandler<UpdateUserPreferenceCommand> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateUserPreferenceCommand(request.UserId, request.Theme, request.ReceiveNotifications);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.UserPreferences);
        }
    }
}
