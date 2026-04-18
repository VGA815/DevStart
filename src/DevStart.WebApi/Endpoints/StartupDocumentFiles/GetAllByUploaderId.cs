using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupDocumentFiles.GetAllByUploaderId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupDocumentFiles
{
    internal sealed class GetAllByUploaderId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/{uploaderId:guid}/documents", async (
                Guid uploaderId,
                IQueryHandler<GetStartupDocumentFilesByUploaderIdQuery, List<StartupDocumentFileResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupDocumentFilesByUploaderIdQuery(uploaderId);

                Result<List<StartupDocumentFileResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupDocumentFiles);
        }
    }
}
