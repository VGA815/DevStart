using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ConsentDocuments.ActivateDocument;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ConsentDocuments
{
    internal sealed class ActivateConsentDocument : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("api/consent-documents/{id:guid}/activate", async (
                Guid id,
                ICommandHandler<ActivateConsentDocumentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ActivateConsentDocumentCommand(id);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .HasPermission(Permissions.ConsentDocumentsActivate)
            .WithTags(Tags.ConsentDocuments);
        }
    }
}
