using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupDocumentFiles.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupDocumentFiles
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/documents/{id:guid}", async (
                Guid id,
                IQueryHandler<GetStartupDocumentFileByIdQuery, StartupDocumentFileResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupDocumentFileByIdQuery(id, 300);

                Result<StartupDocumentFileResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.StartupDocumentFiles);
        }
    }
}
