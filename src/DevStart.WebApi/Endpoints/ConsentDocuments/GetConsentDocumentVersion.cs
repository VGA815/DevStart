using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ConsentDocuments.GetDocument;
using DevStart.Application.ConsentDocuments.GetDocuments;
using DevStart.Domain.UserConsents;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ConsentDocuments
{
    internal sealed class GetConsentDocumentVersion : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/consent-documents/{type:int}/{version}", async (
                int type,
                string version,
                IQueryHandler<GetConsentDocumentQuery, ConsentDocumentResponse> handler,
                CancellationToken cancellationToken) =>
            {
                if (!Enum.IsDefined(typeof(ConsentType), type))
                {
                    return Results.BadRequest($"Invalid consent type: {type}");
                }

                var query = new GetConsentDocumentQuery((ConsentType)type, version);

                Result<ConsentDocumentResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.ConsentDocuments)
            .AllowAnonymous();
        }
    }
}
