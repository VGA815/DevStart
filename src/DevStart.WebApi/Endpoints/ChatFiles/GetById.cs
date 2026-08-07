using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ChatFiles;
using DevStart.Application.ChatFiles.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ChatFiles
{
    internal sealed class GetById : IEndpoint
    {
        private const int PresignedUrlExpirySeconds = 3600;

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/chat/files/{fileId:guid}", async (
                Guid fileId,
                IQueryHandler<GetChatFileByIdQuery, ChatFileResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetChatFileByIdQuery(fileId, PresignedUrlExpirySeconds);

                Result<ChatFileResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ChatFilesRead)
                .WithTags(Tags.ChatFiles);
        }
    }
}
