using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupDocumentFiles.GetAllByStartupId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupDocumentFiles
{
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/documents", async (
                Guid startupId,
                IQueryHandler<GetStartupDocumentFilesByStartupIdQuery, List<StartupDocumentFileResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupDocumentFilesByStartupIdQuery(startupId);

                Result<List<StartupDocumentFileResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.StartupDocumentFiles);
        }
    }
}
