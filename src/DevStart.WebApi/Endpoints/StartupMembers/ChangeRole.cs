
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupMembers.ChangeRole;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupMembers
{
    internal sealed class ChangeRole : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("profile_id")] Guid ProfileId,
            [property: JsonPropertyName("role")] StartupRole Role);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("startups/members/role", async (
                [FromBody] Request request, 
                ICommandHandler<ChangeStartupMemberRoleCommand> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new ChangeStartupMemberRoleCommand(request.StartupId, request.ProfileId, request.Role);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupMembers);
        }
    }
}
