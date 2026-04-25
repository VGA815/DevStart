using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentApplications.Create;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.InvestmentApplications
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("roadmap_item_id")] Guid? RoadmapItemId,
            [property: JsonPropertyName("amount")] decimal Amount,
            [property: JsonPropertyName("message")] string? Message);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/investment-applications", async (
                [FromBody] Request request,
                ICommandHandler<CreateInvestmentApplicationCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateInvestmentApplicationCommand(
                    request.StartupId,
                    request.RoadmapItemId,
                    request.Amount,
                    request.Message);

                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentApplicationsCreate)
                .WithTags(Tags.InvestmentApplications);
        }
    }
}
