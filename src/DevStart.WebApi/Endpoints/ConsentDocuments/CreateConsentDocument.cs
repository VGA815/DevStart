using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ConsentDocuments.CreateDocument;
using DevStart.Domain.UserConsents;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.ConsentDocuments
{
    internal sealed class CreateConsentDocument : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("type")]    ConsentType Type,
            [property: JsonPropertyName("version")] string Version,
            [property: JsonPropertyName("title")]   string Title,
            [property: JsonPropertyName("content")] string Content);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/consent-documents", async (
                Request request,
                ICommandHandler<CreateConsentDocumentCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateConsentDocumentCommand(
                    request.Type,
                    request.Version,
                    request.Title,
                    request.Content);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .HasPermission(Permissions.ConsentDocumentsCreate)
            .WithTags(Tags.ConsentDocuments);
        }
    }
}
