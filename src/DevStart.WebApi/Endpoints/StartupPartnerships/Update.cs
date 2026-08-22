using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupPartnerships.Update;
using DevStart.Domain.StartupPartnerships;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupPartnerships
{
    internal sealed class Update : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("partner_name")] string PartnerName,
            [property: JsonPropertyName("website")] string Website,
            [property: JsonPropertyName("kind")] PartnershipKind Kind,
            [property: JsonPropertyName("description")] string? Description);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/startup-partnerships/{partnershipId:guid}", async (
                Guid partnershipId,
                [FromBody] Request request,
                ICommandHandler<UpdateStartupPartnershipCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateStartupPartnershipCommand(
                    partnershipId,
                    request.PartnerName,
                    request.Website,
                    request.Kind,
                    request.Description);

                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupPartnershipsUpdate)
                .WithTags(Tags.StartupPartnerships);
        }
    }
}
