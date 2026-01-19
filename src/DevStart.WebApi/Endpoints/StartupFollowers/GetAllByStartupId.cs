
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupFollowers.GetAllByStartupId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.StartupFollowers
{
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("startups/followers", async (
                [FromQuery] Guid startupId,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IQueryHandler<GetStartupFollowersByStartupIdQuery, List<StartupFollowerResponse>> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupFollowersByStartupIdQuery(startupId, page, pageSize);

                Result<List<StartupFollowerResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.StartupFollowers);
        }
    }
}
