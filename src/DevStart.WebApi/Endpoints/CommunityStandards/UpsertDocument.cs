using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.CommunityStandards.UpsertDocument;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.CommunityStandards
{
    internal sealed class UpsertDocument : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("title")]   string Title,
            [property: JsonPropertyName("content")] string Content);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // PUT rather than POST: a startup has at most one document per type, so writing is idempotent
            // and the type in the route is the whole identity.
            app.MapPut("api/startups/{startupId:guid}/community/documents/{type}", async (
                Guid startupId,
                CommunityDocumentType type,
                Request request,
                ICommandHandler<UpsertStartupCommunityDocumentCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpsertStartupCommunityDocumentCommand(
                    startupId, type, request.Title, request.Content);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.CommunityDocumentsManage)
                .WithTags(Tags.CommunityStandards);
        }
    }
}
