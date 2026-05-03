using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring;
using DevStart.Application.Startups.GetScore;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Startups
{
    internal sealed class GetScore : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/score", async (
                Guid startupId,
                IQueryHandler<GetStartupScoreQuery, ScoreResult> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupScoreQuery(startupId);
                Result<ScoreResult> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupsScoreRead)
                .WithTags(Tags.Startups);
        }
    }
}
