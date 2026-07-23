using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.CommunityStandards.GetDocument;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.CommunityStandards
{
    internal sealed class GetDocument : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/community/documents/{type}", async (
                Guid startupId,
                CommunityDocumentType type,
                IQueryHandler<GetStartupCommunityDocumentQuery, CommunityDocumentResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupCommunityDocumentQuery(startupId, type);

                Result<CommunityDocumentResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.CommunityStandards);
        }
    }
}
