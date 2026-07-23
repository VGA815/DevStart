using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.CommunityStandards.GetDocuments;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.CommunityStandards
{
    internal sealed class GetDocuments : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/community/documents", async (
                Guid startupId,
                IQueryHandler<GetStartupCommunityDocumentsQuery, List<CommunityDocumentSummary>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupCommunityDocumentsQuery(startupId);

                Result<List<CommunityDocumentSummary>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.CommunityStandards);
        }
    }
}
