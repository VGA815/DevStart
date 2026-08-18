using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ScoringReports.GetScoringReportDownloadUrl;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Startups
{
    internal sealed class GetScoringReportDownloadUrl : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/score/report", async (
                Guid startupId,
                IQueryHandler<GetScoringReportDownloadUrlQuery, ScoringReportDownloadUrlResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetScoringReportDownloadUrlQuery(startupId);
                Result<ScoringReportDownloadUrlResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                // Same permission as the on-screen score: the document is the same information, and
                // the Pro/entitlement gate inside the handler is what actually separates viewers.
                .HasPermission(Permissions.StartupsScoreRead)
                .WithTags(Tags.Startups);
        }
    }
}
