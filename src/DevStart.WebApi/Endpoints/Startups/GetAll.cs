using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups.GetAll;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Startups
{
    internal sealed class GetAll : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("startups", async (
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IQueryHandler <GetStartupsQuery, List <StartupResponse>> handler,
                CancellationToken cancellationToken,
                [FromQuery] StartupStage? stage = null,
                [FromQuery] StartupLocation? location = null,
                [FromQuery] bool? isStopped = null) =>
            {
                GetStartupsQuery query = new(page, pageSize, stage, location, isStopped);

                Result<List<StartupResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Startups);
        }
    }
}
