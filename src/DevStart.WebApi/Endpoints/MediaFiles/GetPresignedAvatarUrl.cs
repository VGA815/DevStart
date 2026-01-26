
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.MediaFiles.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.MediaFiles
{
    internal sealed class GetPresignedAvatarUrl : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("users/avatars/{avatarId:guid}", async (
                Guid avatarId, 
                IQueryHandler<GetMediaFileByIdQuery, MediaFileResponse> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetMediaFileByIdQuery(avatarId, 600);

                Result<MediaFileResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.MediaFiles);
        }
    }
}
