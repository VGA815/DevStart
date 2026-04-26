using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupCompetitors.GetAllByStartupId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupCompetitors
{
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/competitors", async (
                Guid startupId,
                IQueryHandler<GetStartupCompetitorsByStartupIdQuery, List<StartupCompetitorResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupCompetitorsByStartupIdQuery(startupId);
                Result<List<StartupCompetitorResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupCompetitorsRead)
                .WithTags(Tags.StartupCompetitors);
        }
    }
}
