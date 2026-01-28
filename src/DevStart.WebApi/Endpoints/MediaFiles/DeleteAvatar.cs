
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.MediaFiles.Delete;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.MediaFiles
{
    internal sealed class DeleteAvatar : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("users/avatars", async (
                [FromQuery] Guid fileId, 
                ICommandHandler<DeleteMediaFileCommand> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteMediaFileCommand(fileId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.MediaFiles);
        }
    }
}
