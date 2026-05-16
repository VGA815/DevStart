using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ConsentDocuments.GetDocuments;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ConsentDocuments
{
    internal sealed class GetConsentDocuments : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/consent-documents", async (
                IQueryHandler<GetConsentDocumentsQuery, List<ConsentDocumentResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetConsentDocumentsQuery();

                Result<List<ConsentDocumentResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.ConsentDocuments)
            .AllowAnonymous();
        }
    }
}
