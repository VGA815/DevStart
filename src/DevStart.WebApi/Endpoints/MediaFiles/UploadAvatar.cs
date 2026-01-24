
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.MediaFiles.Upload;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.MediaFiles
{
    internal sealed class UploadAvatar : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("users/avatars", async (IFormFile file, [FromQuery] Guid ownerId, ICommandHandler<UploadMediaFileCommand, Guid> handler, CancellationToken cancellationToken) =>
            {
                await using var stream = file.OpenReadStream();

                var command = new UploadMediaFileCommand(
                    ownerId,
                    stream,
                    file.ContentType,
                    file.Length,
                    "avatars");
                    

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .DisableAntiforgery()
                .WithTags(Tags.MediaFiles);
        }
    }
}
