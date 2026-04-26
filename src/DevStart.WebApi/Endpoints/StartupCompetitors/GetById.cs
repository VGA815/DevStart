using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupCompetitors.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupCompetitors
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startup-competitors/{competitorId:guid}", async (
                Guid competitorId,
                IQueryHandler<GetStartupCompetitorByIdQuery, StartupCompetitorResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupCompetitorByIdQuery(competitorId);
                Result<StartupCompetitorResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupCompetitorsRead)
                .WithTags(Tags.StartupCompetitors);
        }
    }
}
