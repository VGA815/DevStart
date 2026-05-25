using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Configuration;
using DevStart.Application.EmailVerificationTokens.VerifyEmail;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DevStart.WebApi.Endpoints.EmailVerificationTokens
{
    internal sealed class Verify : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/verify", async (
                [FromQuery] Guid token,
                IQueryHandler<VerifyEmailQuery, EmailVerificationResponse> innerHandler,
                IOptions<FrontendOptions> frontendOptions,
                CancellationToken cancellationToken) =>
            {
                var query = new VerifyEmailQuery(token);
                Result<EmailVerificationResponse> result = await innerHandler.Handle(query, cancellationToken);

                // The link is opened directly from the user's email client, so we redirect to a
                // friendly SPA page instead of returning raw JSON. A relative path is used when no
                // frontend base URL is configured (SPA served same-origin as the API).
                string baseUrl = frontendOptions.Value.BaseUrl.TrimEnd('/');
                string status = result.IsSuccess ? "success" : "error";

                return Results.Redirect($"{baseUrl}/email/confirmed?status={status}");
            })
                .WithTags(Tags.EmailVerification)
                .WithName("VerifyEmail");
        }
    }
}
