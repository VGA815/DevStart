using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ChatFiles;
using DevStart.Application.ChatFiles.Upload;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ChatFiles
{
    internal sealed class Upload : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/chat/files", async (
                IFormFile file,
                ICommandHandler<UploadChatFileCommand, ChatFileResponse> handler,
                CancellationToken cancellationToken) =>
            {
                await using Stream stream = file.OpenReadStream();

                var command = new UploadChatFileCommand(
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length);

                Result<ChatFileResponse> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ChatFilesUpload)
                .RequireRateLimiting("per-user")
                .DisableAntiforgery()
                .WithTags(Tags.ChatFiles);
        }
    }
}
