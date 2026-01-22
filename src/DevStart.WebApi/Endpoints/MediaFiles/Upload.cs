
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.MediaFiles.Upload;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.MediaFiles
{
    internal sealed class Upload : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("files/media", async (IFormFile file, [FromQuery] Guid ownerId, ICommandHandler<UploadMediaFileCommand, Guid> handler, CancellationToken cancellationToken) =>
            {
                await using var stream = file.OpenReadStream();

                var command = new UploadMediaFileCommand(
                    ownerId,
                    stream,
                    file.ContentType,
                    file.Length);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.MediaFiles);
        }
    }
}
