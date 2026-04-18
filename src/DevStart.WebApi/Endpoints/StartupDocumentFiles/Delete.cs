using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupDocumentFiles.Delete;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.StartupDocumentFiles
{
    internal sealed class Delete : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/startups/documents", async (
                [FromQuery] Guid documentId,
                ICommandHandler<DeleteStartupDocumentFileCommand> handler,
                CancellationToken cancellationToken) => 
            { 
                var command = new DeleteStartupDocumentFileCommand(documentId);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupDocumentFiles);
        }
    }
}
