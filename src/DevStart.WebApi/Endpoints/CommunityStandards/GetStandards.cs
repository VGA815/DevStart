using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.CommunityStandards;
using DevStart.Application.CommunityStandards.GetStandards;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.CommunityStandards
{
    internal sealed class GetStandards : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Anonymous by design: the checklist is a public trust signal, the same as the startup
            // catalog entry it belongs to.
            app.MapGet("api/startups/{startupId:guid}/community", async (
                Guid startupId,
                IQueryHandler<GetStartupCommunityStandardsQuery, CommunityStandardsResult> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupCommunityStandardsQuery(startupId);

                Result<CommunityStandardsResult> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.CommunityStandards);
        }
    }
}
