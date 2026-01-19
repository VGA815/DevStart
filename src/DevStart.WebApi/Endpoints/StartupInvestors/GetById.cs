
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupInvestors.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.StartupInvestors
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("startups/investors", async (
                [FromQuery] Guid profileId, 
                [FromQuery] Guid startupId, 
                IQueryHandler<GetStartupInvestorByIdQuery, StartupInvestorResponse> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupInvestorByIdQuery(startupId, profileId);

                Result<StartupInvestorResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.StartupInvestors);
        }
    }
}
