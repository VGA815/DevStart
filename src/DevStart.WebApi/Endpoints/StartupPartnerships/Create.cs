using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupPartnerships.Create;
using DevStart.Domain.StartupPartnerships;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupPartnerships
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("partner_name")] string PartnerName,
            [property: JsonPropertyName("website")] string Website,
            [property: JsonPropertyName("kind")] PartnershipKind Kind,
            [property: JsonPropertyName("description")] string? Description);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/startup-partnerships", async (
                [FromBody] Request request,
                ICommandHandler<CreateStartupPartnershipCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateStartupPartnershipCommand(
                    request.StartupId,
                    request.PartnerName,
                    request.Website,
                    request.Kind,
                    request.Description);

                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupPartnershipsCreate)
                .WithTags(Tags.StartupPartnerships);
        }
    }
}
