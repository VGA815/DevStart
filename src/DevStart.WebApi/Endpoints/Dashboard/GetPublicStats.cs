using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Dashboard.GetPublicStats;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Dashboard
{
    internal sealed class GetPublicStats : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/stats", async (
                IQueryHandler<GetPublicStatsQuery, PublicStatsResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<PublicStatsResponse> result = await handler.Handle(new GetPublicStatsQuery(), cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Dashboard)
                .AllowAnonymous();
        }
    }
}
