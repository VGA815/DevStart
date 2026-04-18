using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupDocumentFiles.Upload;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.StartupDocumentFiles
{
    internal sealed class Upload : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/startups/documents", async (
                IFormFile file,
                [FromQuery] Guid startupId,
                [FromQuery] string documentName,
                [FromQuery] StartupDocumentType documentType,
                ICommandHandler<UploadStartupDocumentFileCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                await using var stream = file.OpenReadStream();

                var command = new UploadStartupDocumentFileCommand(
                    startupId, documentType, file.Length, stream, file.ContentType, "startup-documents", documentName);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .DisableAntiforgery()
                .WithTags(Tags.StartupDocumentFiles)
                .RequireAuthorization();
        }
    }
}
