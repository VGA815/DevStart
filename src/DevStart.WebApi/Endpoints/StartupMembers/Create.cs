
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupMembers.Create;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupMembers
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("profile_id")] Guid ProfileId,
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("role")] StartupRole Role,
            [property: JsonPropertyName("is_public")] bool IsPublic);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("startups/members", async (
                [FromBody] Request request, 
                ICommandHandler<CreateStartupMemberCommand, Guid> handler, 
                CancellationToken cancellationToken) =>
            { 
                var command = new CreateStartupMemberCommand(request.ProfileId, request.StartupId, request.Role, request.IsPublic);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupMembers);
        }
    }
}
