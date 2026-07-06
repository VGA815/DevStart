using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.OAuth;
using DevStart.Application.Auth.OAuth.Complete;
using DevStart.Application.Users.Register;
using DevStart.Domain.UserConsents;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Auth
{
    internal sealed class OAuthComplete : IEndpoint
    {
        public sealed record ConsentRequest(
            [property: JsonPropertyName("type")] ConsentType Type,
            [property: JsonPropertyName("document_version")] string DocumentVersion,
            [property: JsonPropertyName("accepted")] bool Accepted);

        public sealed record Request(
            [property: JsonPropertyName("pending_token")] string PendingToken,
            [property: JsonPropertyName("consents")] List<ConsentRequest> Consents);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/auth/oauth/complete", async (
                [FromBody] Request request,
                HttpContext httpContext,
                ICommandHandler<CompleteOAuthRegistrationCommand, OAuthAuthResult> handler,
                CancellationToken cancellationToken) =>
            {
                string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
                string? ua = httpContext.Request.Headers.UserAgent.ToString();

                List<ConsentItem> consents = (request.Consents ?? [])
                    .Select(c => new ConsentItem(c.Type, c.DocumentVersion, c.Accepted))
                    .ToList();

                var command = new CompleteOAuthRegistrationCommand(request.PendingToken, consents, ip, ua);

                Result<OAuthAuthResult> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Auth)
            .RequireRateLimiting("auth");
        }
    }
}
