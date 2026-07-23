using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.CommunityStandards.DeleteDocument;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.CommunityStandards
{
    internal sealed class DeleteDocument : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/startups/{startupId:guid}/community/documents/{type}", async (
                Guid startupId,
                CommunityDocumentType type,
                ICommandHandler<DeleteStartupCommunityDocumentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteStartupCommunityDocumentCommand(startupId, type);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.CommunityDocumentsManage)
                .WithTags(Tags.CommunityStandards);
        }
    }
}
